import torch
import sys
import re
from diffusers import StableDiffusionPipeline, DPMSolverMultistepScheduler

model_id = "stabilityai/stable-diffusion-2-1"

# Read parameters from command line
request_guid = sys.argv[1]
hf_token = sys.argv[2]

# Load text prompt into file
file = open(f".{chr(92)}Python{chr(92)}{request_guid}.txt")
prompt = file.read()
file.close()

negative_arr = re.findall("\(+.*?\)+", prompt)
prompt = re.sub("\(+.*?\)+", "", prompt)
prompt_negative = ' '.join(negative_arr).replace("(", "").replace(")", "")

print(f'Prompt is: {prompt}')
print(f'Negative Prompt is: {prompt_negative}')

# Use the DPMSolverMultistepScheduler (DPM-Solver++) scheduler here instead
pipe = StableDiffusionPipeline.from_pretrained(model_id, torch_dtype=torch.float16, use_auth_token=hf_token, cache_dir=".\cache")
pipe.scheduler = DPMSolverMultistepScheduler.from_config(pipe.scheduler.config)
pipe = pipe.to("cuda")

image = pipe(prompt, negative_prompt=prompt_negative).images[0]
    
image.save(f'.{chr(92)}Python{chr(92)}{request_guid}.png')
