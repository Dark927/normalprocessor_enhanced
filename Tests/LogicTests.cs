using NUnit.Framework;
using UnityEngine;
using dev.sudohub.normalprocessor;
using System;

namespace NormalMapGeneratorTests
{
    [TestFixture]
    public class LogicTests
    {
        #region Fields 

        private const float ParametersMinValue = 0f;
        private const float ParametersMaxValue = 20f;


        private NormalProcessorGPU _processor;
        private Texture2D _testTexture;
        private AnimationCurve _testCurve;
        private NormalProcessorArrayState _arrayState;

        #endregion


        #region Setup

        [SetUp]
        public void SetUp()
        {
            _testTexture = new Texture2D(256, 256);
            _processor = new NormalProcessorGPU();
            _testCurve = AnimationCurve.Linear(0, 0, 1, 1);
            _arrayState = new NormalProcessorArrayState(Vector2Int.one);
        }

        #endregion


        #region A4 : Import Test

        [Test]
        [TestCase(1, 1)]
        [TestCase(512, 512)]
        [TestCase(5000, 5000)]
        [TestCase(10001, 5)]
        [TestCase(16000, 16000)]
        public void TestRebindTexture(int width, int height)
        {
            var newTexture = new Texture2D(width, height);
            _processor.RebindTexture(newTexture);
            Assert.AreEqual(newTexture, _processor.InputTexture, "The texture was not imported correctly.");

            var result = _processor.GetTexture();
            Assert.AreNotEqual(null, result, "The normal map was not created correctly.");
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void TestNullImportTexture(bool preimportCorrectTex)
        {
            if (preimportCorrectTex)
            {
                _processor.RebindTexture(_testTexture);
            }

            _processor.RebindTexture(null);
            Assert.AreEqual(null, _processor.InputTexture, "The texture was not cleared correctly.");

            var result = _processor.GetTexture();
            Assert.AreEqual(null, result, "Imported texture is null - Result must be null! ");
        }

        #endregion


        #region A6 : Grid Size Test

        [Test]
        [TestCase(1, 1)]
        [TestCase(4, 12)]
        [TestCase(16, 1)]
        [TestCase(16, 16)]
        public void TestCorrectGridResizing(int width, int height)
        {
            Vector2Int size = new Vector2Int(width, height);
            _arrayState.Resize(size);

            Assert.AreEqual(size, _arrayState.Size, "The grid size was not set correctly.");
        }

        [Test]
        [TestCase(-1, -1)]
        [TestCase(0, 0)]
        [TestCase(512, 512)]
        [TestCase(5000, 5000)]
        [TestCase(16000, 16000)]
        public void TestIncorrectGridResizing(int width, int height)
        {
            Vector2Int size = new Vector2Int(width, height);
            Assert.DoesNotThrow(() => _arrayState.Resize(size), "Resizing throws an exception! Incorrect values must be handled.");
            Assert.AreNotEqual(size, _arrayState.Size, "The grid size was set with wrong size.");
        }

        #endregion


        #region A8-A9 : Generation Parameters/Process Test

        [Test]
        [TestCase(0f, 0f, true, true)]
        [TestCase(10f, 10f, true, false)]
        [TestCase(10f, 10f, false, true)]
        [TestCase(30f, 30f, false, false)]
        [TestCase(-1f, -1f, true, true)]
        //[TestCase(100000, 100000, true, false)]
        [TestCase(100f, 100f, true, false)]
        [TestCase(-5f, 0.001f, false, true)]
        [TestCase(0f, -0.001f, false, false)]
        public void TestNormalMapGenerationParameters(float smoothness, float intensity, bool doTiling, bool useScharr)
        {
            _processor.RebindTexture(_testTexture);

            if (smoothness < ParametersMinValue || smoothness > ParametersMaxValue)
            {
                Assert.Throws<Exception>(() => _processor.ComputeGauss(smoothness), "Method must throw an exception with incorrect smoothness!");
            }
            else
            {
                Assert.DoesNotThrow(() => _processor.ComputeGauss(smoothness), "Method should not throw an exception for valid smoothness.");
            }

            if (intensity < ParametersMinValue || intensity > ParametersMaxValue)
            {
                Assert.Throws<Exception>(() => _processor.ComputeNormal(intensity), "Method must throw an exception with incorrect intensity!");
            }
            else
            {
                Assert.DoesNotThrow(() => _processor.ComputeNormal(intensity), "Method should not throw an exception for valid intensity.");
            }

            Assert.DoesNotThrow(() => _processor.UpdateKeywords(doTiling, useScharr), "Method must not throw an exception");

            if (doTiling)
            {
                TestTilingSetup();
            }

            Texture2D result = _processor.GetTexture();

            Assert.AreNotEqual(null, result, "The normal map was not created correctly.");
        }

        private void TestTilingSetup()
        {
            Vector2Int targetTilingSize = Vector2Int.one;
            Vector2Int targetTilingOffset = Vector2Int.zero;

            Assert.DoesNotThrow(() => _processor.SetTiling(targetTilingSize, targetTilingOffset),
                "Method must not throw an exceptions with correct intensity!");

            var tilingSizeFieldInfo = typeof(NormalProcessorGPU).GetField("tileSize",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var tilingOffsetFieldInfo = typeof(NormalProcessorGPU).GetField("tileOffset",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Vector2Int tilingSize = (Vector2Int)tilingSizeFieldInfo.GetValue(_processor);
            Vector2Int tilingOffset = (Vector2Int)tilingOffsetFieldInfo.GetValue(_processor);

            Assert.AreEqual(targetTilingSize, tilingSize);
            Assert.AreEqual(targetTilingOffset, tilingOffset);
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
