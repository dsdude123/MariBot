import torch
import random
import sys
from diffusers import StableDiffusionPipeline
from diffusers.pipelines.stable_diffusion.safety_checker import (
    StableDiffusionSafetyChecker,
)
from torch.cuda.amp import autocast

# Could probably use an inline lambda for this
def dummy(images, **kwargs):
    return images, False

# Read parameters from command line
request_guid = sys.argv[1]

# Load text prompt into file
file = open(f"{request_guid}.txt")
prompt = file.read()
file.close()

# I only have 8GB of VRAM on my 3070, so in order to fit the model in memory 
# we create the pipeline using float16, rather than the default of float32.
pipe = StableDiffusionPipeline.from_pretrained("CompVis/stable-diffusion-v1-4",
                                                revision="fp16",
                                                torch_dtype=torch.float16,
                                                use_auth_token="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")
pipe.to("cuda")                 # Run on GPU
#pipe.safety_checker = dummy    # Disable NSFW check

with autocast(True):
	image = pipe(prompt).images[0]
	image.save(f'{request_guid}.png')