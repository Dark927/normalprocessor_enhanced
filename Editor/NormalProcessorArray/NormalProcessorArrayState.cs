using System;
using UnityEngine;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using UnityEditor;
using System.Linq;
using UnityEditor.Search;

namespace dev.sudohub.normalprocessor
{

    [Serializable]
    public record PresetContainer
    {
        public Vector2Int Size;
        public Preset[] presets;
        public PresetContainer(Vector2Int size, Preset[] presets){
            this.Size = size;
            this.presets = presets;
        }
    }

    public class NormalProcessorArrayState : INotifyPropertyChanged
    {
        public Vector2Int ActiveCoord
        {
            get => _activeCoord;
            set
            {
                _activeCoord = value;
                _changes |= Changes.Everything;
                OnPropertyChanged();
                //OnNeedsCompute();
            }
        }
        private Texture2D _inputTexture;
        public Texture2D InputTexture
        {
            get => _inputTexture;
            set
            {
                _inputTexture = value;
                if (_inputTexture != null)
                    TryLoadPresetInfo();

                OnNeedsCompute();
            }
        }

        private Changes _changes = Changes.Everything; //changes of active sub tile;
        public Changes Changes
        {
            get => _changes;
            set
            {
                _changes = value;
                if (doLockParams)
                    FillPresets(CurrentPreset);
                if (_changes != Changes.None)
                    OnNeedsCompute();
            }
        }
        //grid
        public Vector2Int Size { get; private set; }
        public int Active => _activeCoord.x + _activeCoord.y * Size.x;
        private Vector2Int _activeCoord = Vector2Int.zero;

        [SerializeField]
        public Preset[] presets; //array of presets for processing
        public ref Preset CurrentPreset => ref presets[Active];
        public bool doLockParams = false;

        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangedEventHandler NeedsCompute;

        public NormalProcessorArrayState(Vector2Int size)
        {
            Resize(size);
        }
        public void Resize(Vector2Int size)
        {
            Size = size;
            presets = new Preset[size.x * size.y];
            for (var i = 0; i < size.x * size.y; i++)
                presets[i] = new("Default Preset");
        }

        public void FillPresets(Preset src)
        {
            for (var i = 0; i < presets.Length; i++)
            {
                presets[i] = src;//pass by value
            }
        }

        //update Options
        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            //Debug.Log("[Normal Processor] ArrayState PropertyChanged <-" + (name ?? ""));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        //update compute
        private void OnNeedsCompute([CallerMemberName] string name = null)
        {
            //Debug.Log("[Normal Processor] ArrayState NeedsCompute <-" + (name ?? ""));
            NeedsCompute?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void SavePresetInfo()
        {
            var texturePath = AssetDatabase.GetAssetPath(InputTexture);
            var json = JsonUtility.ToJson(new PresetContainer(Size, presets), prettyPrint: true);
            texturePath += ".normproc";
            File.WriteAllText(texturePath, json);
        }

        public void TryLoadPresetInfo()
        {
            if (InputTexture == null) return;
            var assetPath = AssetDatabase.GetAssetPath(InputTexture) + ".normproc";

            if (File.Exists(assetPath))
            {
                var json = File.ReadAllText(assetPath);
                var data = JsonUtility.FromJson<PresetContainer>(json);
                //check data integrity
                if (data.presets == null || data.presets.Length != data.Size.x * data.Size.y)
                    return;
                Size = data.Size;
                presets = data.presets;
                Debug.Log("[Normal Processor] Loaded previously saved Preset Info.");
            }
        }
    }
}
