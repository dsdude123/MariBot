import os
import sys

import torch
from diffusers import DDIMScheduler, StableDiffusionPipeline
from torch import autocast

from _paths import model_cache, read_prompt

model_id = "hakurei/waifu-diffusion"
device = "cuda"

os.environ["PYTORCH_CUDA_ALLOC_CONF"] = "max_split_size_mb:128"

prompt_path = sys.argv[1]
output_path = sys.argv[2]

prompt = read_prompt(prompt_path)

pipe = StableDiffusionPipeline.from_pretrained(
    model_id,
    torch_dtype=torch.float16,
    cache_dir=model_cache(),
    scheduler=DDIMScheduler(
        beta_start=0.00085,
        beta_end=0.012,
        beta_schedule="scaled_linear",
        clip_sample=False,
        set_alpha_to_one=False,
    ),
)
pipe = pipe.to(device)

with autocast("cuda"):
    image = pipe(prompt).images[0]
    image.save(output_path)
