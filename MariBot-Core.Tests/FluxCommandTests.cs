using System;
using System.Threading.Tasks;
using MariBot.Core.Models.BlackForestLabs.Flux;
using MariBot.Core.Services;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace MariBot.Core.Tests
{
    /// <summary>
    /// That flux and flux1 reach different Flux generations, and that each sends
    /// the request shape its endpoint expects.
    /// </summary>
    public class FluxCommandTests : AiModuleTestBase
    {
        [Fact]
        public async Task FluxRunsTheCurrentModelAndFlux1ThePreviousOne()
        {
            var result = new ImageResponse.Result { sample = "https://example.invalid/i.jpg", prompt = "p" };
            MockFlux.Setup(s => s.GenerateFlux(It.IsAny<string>())).ReturnsAsync(result);
            MockFlux.Setup(s => s.GenerateFlux1(It.IsAny<string>())).ReturnsAsync(result);
            MockImage.Setup(s => s.GetWebResource(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("no network in tests"));

            await Module.FluxImageGeneration("a cat");
            await Module.Flux1ImageGeneration("a cat");

            MockFlux.Verify(s => s.GenerateFlux("a cat"), Times.Once());
            MockFlux.Verify(s => s.GenerateFlux1("a cat"), Times.Once());
        }

        [Fact]
        public void FluxEndpointsPointAtTheCurrentHostAndModels()
        {
            // Pinned because a wrong host or model slug fails only in production,
            // and BFL moved off the api.bfl.ml domain this used to call.
            Assert.Equal("https://api.bfl.ai/v1/flux-2-max", FluxService.Flux2MaxEndpoint);
            Assert.Equal("https://api.bfl.ai/v1/flux-pro-1.1", FluxService.Flux1ProEndpoint);
            Assert.Equal("https://api.bfl.ai/v1/get_result", FluxService.ResultEndpoint);
        }

        [Fact]
        public void TheTwoFluxGenerationsUseTheirOwnPromptUpsamplingField()
        {
            // FLUX.2 renamed this control and inverted it. Sending one API the
            // other's field name silently loses the setting.
            var flux2 = JsonConvert.SerializeObject(new Flux2ImageRequest { prompt = "p" });
            var flux1 = JsonConvert.SerializeObject(new ImageRequest { prompt = "p" });

            Assert.Contains("disable_pup", flux2);
            Assert.DoesNotContain("prompt_upsampling", flux2);

            Assert.Contains("prompt_upsampling", flux1);
            Assert.DoesNotContain("disable_pup", flux1);
        }
    }
}
