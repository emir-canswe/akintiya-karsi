using NUnit.Framework;
using Upstream.Core;
using Upstream.UI;

namespace Upstream.Tests
{
    public class ControlsAndDisplayTests
    {
        [Test]
        public void Sensitivity_HigherLevelsMoveFurtherPerSlide()
        {
            Assert.Less(ControlSettings.GainFor(ControlSettings.Low), ControlSettings.GainFor(ControlSettings.Medium));
            Assert.Less(ControlSettings.GainFor(ControlSettings.Medium), ControlSettings.GainFor(ControlSettings.High));
            Assert.Greater(ControlSettings.GainFor(ControlSettings.Low), 1f, "Even Low is a bit quicker than the finger.");
            // Out-of-range saved values don't crash.
            Assert.AreEqual(ControlSettings.GainFor(ControlSettings.High), ControlSettings.GainFor(99));
            Assert.AreEqual(ControlSettings.GainFor(ControlSettings.Low), ControlSettings.GainFor(-5));
        }

        [Test]
        public void FrameRate_FollowsTheScreen()
        {
            Assert.AreEqual(60, UpstreamGameBootstrap.TargetFrameRateFor(60.0));
            Assert.AreEqual(90, UpstreamGameBootstrap.TargetFrameRateFor(90.0), "90 Hz phones run at 90 (60 would judder).");
            Assert.AreEqual(120, UpstreamGameBootstrap.TargetFrameRateFor(120.0));
            Assert.AreEqual(120, UpstreamGameBootstrap.TargetFrameRateFor(144.0));
            Assert.AreEqual(60, UpstreamGameBootstrap.TargetFrameRateFor(0.0));
            Assert.AreEqual(60, UpstreamGameBootstrap.TargetFrameRateFor(double.NaN));
        }

        [Test]
        public void Ui_PhonesKeepTheDesign_TabletsMatchHeight()
        {
            Assert.AreEqual(0.5f, AdaptiveCanvasScaler.MatchFor(1080f / 2340f), "tall phone");
            Assert.AreEqual(0.5f, AdaptiveCanvasScaler.MatchFor(9f / 16f), "16:9 phone");
            Assert.AreEqual(1f, AdaptiveCanvasScaler.MatchFor(3f / 4f), "iPad");
            Assert.AreEqual(1f, AdaptiveCanvasScaler.MatchFor(10f / 16f), "Android tablet");
        }
    }
}
