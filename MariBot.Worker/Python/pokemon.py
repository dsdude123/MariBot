import os
import sys

import torch
from diffusers import StableDiffusionPipeline
from torch import autocast

from _paths import model_cache, read_prompt

os.environ["PYTORCH_CUDA_ALLOC_CONF"] = "max_split_size_mb:128"

prompt_path = sys.argv[1]
output_path = sys.argv[2]

pipe = StableDiffusionPipeline.from_pretrained(
    "lambdalabs/sd-pokemon-diffusers",
    torch_dtype=torch.float16,
    cache_dir=model_cache(),
)
pipe = pipe.to("cuda")

prompt = read_prompt(prompt_path)

scale = 10
n_samples = 1

# The NSFW checker mistakes Pokémon for its own trigger often enough to be
# useless here; prompts are moderated before they reach this script.
disable_safety = True

print(f"Input prompt is {prompt}")
print(f"Output file is {output_path}")

if disable_safety:

    def null_safety(images, **kwargs):
        return images, False

    pipe.safety_checker = null_safety

with autocast("cuda"):
    images = pipe(n_samples * [prompt], guidance_scale=scale).images

for image in images:
    image.save(output_path)
