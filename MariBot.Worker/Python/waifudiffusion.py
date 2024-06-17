import os
import sys
import torch
from torch import autocast
from diffusers import StableDiffusionPipeline, DDIMScheduler

model_id = "hakurei/waifu-diffusion"
device = "cuda"

os.environ['PYTORCH_CUDA_ALLOC_CONF'] = 'max_split_size_mb:128'

# Read parameters from command line
request_guid = sys.argv[1]

# Load text prompt into file
file = open(f".{chr(92)}Python{chr(92)}{request_guid}.txt","r",encoding='utf-8')
prompt = file.read()
file.close()

pipe = StableDiffusionPipeline.from_pretrained(
    model_id,
    torch_dtype=torch.float16,
    revision="fp16",
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
	image.save(f'.{chr(92)}Python{chr(92)}{request_guid}.png')