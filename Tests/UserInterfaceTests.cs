

using dev.sudohub.normalprocessor;
using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NormalMapGeneratorTests
{
    [TestFixture]
    public class UserInterfaceTests
    {
        #region Fields

        private const string TestTexturePath = "Assets/TestTexture.png";
        private string NormalMapDefaultSavePath => TestTexturePath.Insert(TestTexturePath.IndexOf('.'), "_Normal");


        private NormalProcessorArrayWindow _utilityWindow;
        private NormalProcessorGPU _normalProcessorLogic;
        private NormalProcessorArrayState _processorArrayState;
        private Texture2D _testTexture;

        private bool _removeAllFilesAfterTest = true;

        #endregion


        #region Setup: Initialize FileRenamerGUI

        [SetUp]
        public void SetUp()
        {
            _utilityWindow = EditorWindow.GetWindow<NormalProcessorArrayWindow>("Test NormalProcessorArrayWindow");
            _utilityWindow.Show();

            var normalProcessorLogicField = GetPrivateFieldWithCheck(typeof(NormalProcessorArrayWindow), "_processor");
            _normalProcessorLogic = ((Lazy<NormalProcessorGPU>)normalProcessorLogicField.GetValue(_utilityWindow)).Value;

            var normalProcessorArrayStateField = GetPrivateFieldWithCheck(typeof(NormalProcessorArrayWindow), "state");
            _processorArrayState = (NormalProcessorArrayState)normalProcessorArrayStateField.GetValue(_utilityWindow);

            _testTexture = CreateTexture(new Vector2Int(512, 512));
        }

        private FieldInfo GetPrivateFieldWithCheck(Type targetType, string fieldName)
        {
            var targetFieldInfo = targetType.GetField(fieldName,
                    BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(targetFieldInfo, $"Could not find {fieldName} field");

            return targetFieldInfo;
        }

        private Texture2D CreateTexture(Vector2Int size)
        {
            // Create a 512x512 texture
            Texture2D texture = new Texture2D(size.x, size.y);

            // Fill texture with random colors
            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    texture.SetPixel(x, y, new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value));
                }
            }
            texture.Apply();

            // Save texture as PNG in Assets folder
            byte[] pngData = texture.EncodeToPNG();
            File.WriteAllBytes(TestTexturePath, pngData);

            // Import the texture asset
            AssetDatabase.ImportAsset(TestTexturePath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            // Get reference to the actual imported texture
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TestTexturePath);
        }

        #endregion


        #region A10 : Preview Window Test

        [Test]
        public void TestPreviewWindowState()
        {
            FieldInfo previewStateField = typeof(NormalProcessorArrayWindow).GetField("previewState",
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.AreNotEqual(null, previewStateField, "PreviewStateField does not exist in the GUI");
            PreviewState previewState = (PreviewState)previewStateField.GetValue(_utilityWindow);
            Assert.AreNotEqual(null, previewState, "PreviewState is null. It must be assigned!");


            _normalProcessorLogic.RebindTexture(_testTexture);
            Assert.AreNotEqual(null, previewState.GetTexture(_normalProcessorLogic), "Preview texture is null. It must be displayed!");

            _normalProcessorLogic.RebindTexture(null);
            Assert.AreEqual(null, previewState.GetTexture(_normalProcessorLogic), "Preview texture is not null. It must be cleared!");
        }

        #endregion


        #region A12 : Export Test

        [Test]
        public void TestCorrectExport()
        {
            BindTextureWithCheck(_normalProcessorLogic, _testTexture);
            _processorArrayState.InputTexture = _testTexture;

            var saveMethodInfo = GetExportMethod(_utilityWindow);

            Assert.AreNotEqual(null, saveMethodInfo, "Export method can not be found in the GUI!");

            Assert.DoesNotThrow(() => saveMethodInfo.Invoke(_utilityWindow, new object[] { }), "Export method must not throw an exception with the correct texture!");
        }

        [Test]
        public void TestExportAfterTextureRebind()
        {
            BindTextureWithCheck(_normalProcessorLogic, _testTexture);

            _normalProcessorLogic.RebindTexture(null);
            _processorArrayState.InputTexture = null;

            var saveMethodInfo = GetExportMethod(_utilityWindow);

            Assert.Catch(() => saveMethodInfo.Invoke(_utilityWindow, new object[] { }), "Export method must throw an exception with the null texture!");
        }

        private void BindTextureWithCheck(NormalProcessorGPU processorLogic, Texture2D texture)
        {
            processorLogic.RebindTexture(texture);

            var targetTexture = processorLogic.GetTexture();
            Assert.AreNotEqual(null, targetTexture, "Binded texture is null. It must be available!");
        }

        private MethodInfo GetExportMethod(NormalProcessorArrayWindow utilityWindow)
        {
            var saveMethodInfo = typeof(NormalProcessorArrayWindow).GetMethod("SaveNormalMap",
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.AreNotEqual(null, saveMethodInfo, "Export method can not be found in the GUI!");

            return saveMethodInfo;
        }

        #endregion


        #region A15 : Export Override Test

        [Test]
        public void TestExportExistingResultOverride()
        {
            _removeAllFilesAfterTest = false;

            BindTextureWithCheck(_normalProcessorLogic, _testTexture);
            _processorArrayState.InputTexture = _testTexture;

            var saveMethodInfo = GetExportMethod(_utilityWindow);

            Assert.AreNotEqual(null, saveMethodInfo, "Export method can not be found in the GUI!");

            if (AssetDatabase.AssetPathExists(NormalMapDefaultSavePath))
            {
                Assert.DoesNotThrow(() => saveMethodInfo.Invoke(_utilityWindow, new object[] { }), "Export method must throw an exception about the override!");
            }
            else
            {
                saveMethodInfo.Invoke(_utilityWindow, new object[] { });
                Assert.DoesNotThrow(() => saveMethodInfo.Invoke(_utilityWindow, new object[] { }), "Export method must not throw an exception when overriding!");
            }
        }

        #endregion


        #region TearDown

        [TearDown]
        public void TearDown()
        {
            RemoveTestTextureWithResults();

            _normalProcessorLogic.Dispose();
            _utilityWindow.Dispose();
            _utilityWindow.Close();
        }

        private void RemoveTestTextureWithResults()
        {
            if (!AssetDatabase.AssetPathExists(TestTexturePath) || !_removeAllFilesAfterTest)
            {
                return;
            }

            AssetDatabase.DeleteAsset(TestTexturePath);
            AssetDatabase.DeleteAsset(TestTexturePath + ".normproc");
            AssetDatabase.DeleteAsset(NormalMapDefaultSavePath);
            Debug.Log("Test texture deleted from: " + TestTexturePath);
            AssetDatabase.Refresh();
        }

        #endregion
    }
}
