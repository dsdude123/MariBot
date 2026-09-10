import re
import sys

import torch
from diffusers import DPMSolverMultistepScheduler, StableDiffusionPipeline

from _paths import model_cache, read_prompt

model_id = "stabilityai/stable-diffusion-2-1"

prompt_path = sys.argv[1]
output_path = sys.argv[2]
hf_token = sys.argv[3] if len(sys.argv) > 3 else None

prompt = read_prompt(prompt_path)

# Anything in parentheses is the negative prompt.
negative_arr = re.findall(r"\(+.*?\)+", prompt)
prompt = re.sub(r"\(+.*?\)+", "", prompt)
prompt_negative = " ".join(negative_arr).replace("(", "").replace(")", "")

print(f"Prompt is: {prompt}")
print(f"Negative Prompt is: {prompt_negative}")

pipe = StableDiffusionPipeline.from_pretrained(
    model_id,
    torch_dtype=torch.float16,
    use_auth_token=hf_token or None,
    cache_dir=model_cache(),
)
pipe.scheduler = DPMSolverMultistepScheduler.from_config(pipe.scheduler.config)
pipe = pipe.to("cuda")

image = pipe(prompt, negative_prompt=prompt_negative).images[0]
image.save(output_path)
