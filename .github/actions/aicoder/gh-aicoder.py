import subprocess
import os

# https://docs.github.com/en/actions/learn-github-actions/finding-and-customizing-actions
def main(mode, prompt, api_key, model, path, template_files):

    # Get inputs from env variables
    mode = os.environ.get("INPUT_MODE").lower()
    prompt = os.environ.get("INPUT_PROMPT", "")
    api_key = os.environ.get("INPUT_API_KEY")
    model = os.environ.get("INPUT_MODEL", "gpt-4")
    path = os.environ.get("CHECK_PATH", "")
    template_files = os.environ.get("TEMPLATE_FILES", "")
    orig_branch = os.environ.get("ORIGIN_BRANCH", "")
    to_branch = os.environ.get("TO_BRANCH", "")

    #Allowed modes
    allowed_modes = ["file-enhancer", "file-generator", "file-security", "file-optimizer", "file-comments", "file-bugfixer"]
    if mode not in allowed_modes:
        raise ValueError(f"Mode must be one of {allowed_modes}")

    # Construct the aicoder command based on the mode
    command = [
        "aicoder",
        mode,
        "--prompt", prompt,
        "--api-key", api_key,
        "--model", model,
        "--orig-branch", orig_branch,
        "--to-branch", to_branch
    ]
    
    if path:
        command.extend(["--path", path])
    elif template_files:
        command.extend(["--template-files", template_files])
    else:
        raise ValueError("Either path or template_files must be provided")

    if path and template_files:
        raise ValueError("Either path or template_files must be provided")

    if template_files and mode != "file-generator":
        raise ValueError("template_files can only be used with file-generator mode")

    # Run the command
    subprocess.run(command)
