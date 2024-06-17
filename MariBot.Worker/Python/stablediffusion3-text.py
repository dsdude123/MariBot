import torch
import sys
import re
from huggingface_hub import login
from diffusers import StableDiffusion3Pipeline

model_id = "stabilityai/stable-diffusion-3-medium-diffusers"

# Read parameters from command line
request_guid = sys.argv[1]
hf_token = sys.argv[2]
login(token=hf_token)

# Load text prompt into file
file = open(f".{chr(92)}Python{chr(92)}{request_guid}.txt","r",encoding='utf-8')
prompt = file.read()
file.close()

negative_arr = re.findall("\(+.*?\)+", prompt)
prompt = re.sub("\(+.*?\)+", "", prompt)
prompt_negative = ' '.join(negative_arr).replace("(", "").replace(")", "")

print(f'Prompt is: {prompt}')
print(f'Negative Prompt is: {prompt_negative}')

# Use the DPMSolverMultistepScheduler (DPM-Solver++) scheduler here instead
pipe = StableDiffusion3Pipeline.from_pretrained(model_id, torch_dtype=torch.float16, cache_dir=".\cache")
# pipe.scheduler = DPMSolverMultistepScheduler.from_config(pipe.scheduler.config)
pipe.to("cuda")

image = pipe(prompt, negative_prompt=prompt_negative, num_inference_steps=28, guidance_scale=7.0).images[0]
    
image.save(f'.{chr(92)}Python{chr(92)}{request_guid}.png')
