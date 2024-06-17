import torch
import os
import sys
from diffusers import StableDiffusionPipeline
from torch import autocast

os.environ['PYTORCH_CUDA_ALLOC_CONF'] = 'max_split_size_mb:128'

pipe = StableDiffusionPipeline.from_pretrained("lambdalabs/sd-pokemon-diffusers", torch_dtype=torch.float16)  
pipe = pipe.to("cuda")

# Read parameters from command line
request_guid = sys.argv[1]

# Load text prompt into file
file = open(f".{chr(92)}Python{chr(92)}{request_guid}.txt","r",encoding='utf-8')
prompt = file.read()
file.close()

scale = 10
n_samples = 1

# Sometimes the nsfw checker is confused by the Pokémon images, you can disable
# it at your own risk here
disable_safety = True

print(f"Input prompt is {prompt}")
print(f"Output directory is {outdir}")

if disable_safety:
  def null_safety(images, **kwargs):
      return images, False
  pipe.safety_checker = null_safety

with autocast("cuda"):
  images = pipe(n_samples*[prompt], guidance_scale=scale).images

for idx, im in enumerate(images):
  im.save(f".{chr(92)}Python{chr(92)}{request_guid}.png")