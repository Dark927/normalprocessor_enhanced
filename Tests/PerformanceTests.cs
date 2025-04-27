using NUnit.Framework;
using UnityEngine;
using dev.sudohub.normalprocessor;
using System.Diagnostics;

namespace NormalMapGeneratorTests
{
    [TestFixture]
    public class PerformanceTests
    {
        #region Fields 

        #region Default Test Parameters 

        private float DefaultTextureIntensity => 7.5f;
        private float DefaultTextureSmoothness => 4.75f;
        private bool UseScharr => true;
        private bool UseTiling => false;
        private AnimationCurve GrayscaleCurve => AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        #endregion


        private NormalProcessorGPU _processor;

        #endregion


        #region Setup

        [SetUp]
        public void SetUp()
        {
            _processor = new NormalProcessorGPU();
        }

        #endregion


        #region Volume Test

        [TestCase(1, 1, TextureFormat.RGBA32)]
        [TestCase(128, 128, TextureFormat.RGBA32)]
        [TestCase(511, 101, TextureFormat.RGBA32)]
        [TestCase(2000, 2000, TextureFormat.RGBA32)]
        [TestCase(2048, 2048, TextureFormat.RGBA32)]
        [TestCase(4001, 4005, TextureFormat.RGBA32)]
        [TestCase(8192, 8192, TextureFormat.RGBA32)]
        [TestCase(12000, 12001, TextureFormat.RGBA32)]
        [TestCase(15999, 15999, TextureFormat.RGBA32)]


        [TestCase(1, 1, TextureFormat.RGB24)]
        [TestCase(128, 128, TextureFormat.RGB24)]
        [TestCase(511, 101, TextureFormat.RGB24)]
        [TestCase(2000, 2000, TextureFormat.RGB24)]
        [TestCase(2048, 2048, TextureFormat.RGB24)]
        [TestCase(4001, 4005, TextureFormat.RGB24)]
        [TestCase(8192, 8192, TextureFormat.RGB24)]
        [TestCase(12000, 12001, TextureFormat.RGB24)]
        [TestCase(15999, 15999, TextureFormat.RGB24)]

        [TestCase(1, 1, TextureFormat.ARGB32)]
        [TestCase(128, 128, TextureFormat.ARGB32)]
        [TestCase(511, 101, TextureFormat.ARGB32)]
        [TestCase(2000, 2000, TextureFormat.ARGB32)]
        [TestCase(2048, 2048, TextureFormat.ARGB32)]
        [TestCase(4001, 4005, TextureFormat.ARGB32)]
        [TestCase(8192, 8192, TextureFormat.ARGB32)]
        [TestCase(12000, 12001, TextureFormat.ARGB32)]
        [TestCase(15999, 15999, TextureFormat.ARGB32)]
        public void TestTextureProcessingTime(int width, int height, TextureFormat format)
        {
            Texture2D texture = GenerateRandomTexture(width, height, format);
            MeasureProcessingTime(texture);
            Assert.NotNull(texture, "Texture must not be null.");
        }

        private Texture2D GenerateRandomTexture(int width, int height, TextureFormat format)
        {
            Texture2D texture = new Texture2D(width, height, format, false);
            Color[] pixels = new Color[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(Random.value, Random.value, Random.value);
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void MeasureProcessingTime(Texture2D texture)
        {
            Stopwatch stopwatch = new Stopwatch();
            Texture2D resultTexture = null;

            stopwatch.Start();

            _processor.RebindTexture(texture);
            _processor.ComputeNormal(DefaultTextureIntensity);
            _processor.ComputeGauss(DefaultTextureSmoothness);
            _processor.ComputeLUT(GrayscaleCurve);
            _processor.UpdateKeywords(UseTiling, UseScharr);
            resultTexture = _processor.GetTexture();

            stopwatch.Stop();

            Assert.NotNull(resultTexture, "Result texture must not be null!");
            UnityEngine.Debug.Log($"Time taken to process {texture.width}x{texture.height} texture: {stopwatch.ElapsedMilliseconds} ms");
        }


        #endregion


        #region TearDown

        [TearDown]
        public void TearDown()
        {
            _processor.Dispose();
        }

        #endregion
    }
}
