import os

def replace_characters(file_path):
    # Define replacements here
    replacements = {
        '“': '',  # Replace left double quotation mark
        '”': '',  # Replace right double quotation mark
        "‘": "'",  # Replace left single quotation mark
        "’": "'",  # Replace right single quotation mark
        "—": "-",  # Replace em dash
        "–": "-",  # Replace en dash
        "…": "..." # Replace ellipsis
    }

    # Read the original file
    with open(file_path, 'r', encoding='utf8') as file:
        content = file.read()

    # Replace problematic characters
    for old, new in replacements.items():
        content = content.replace(old, new)

    # Write the modified content back to the file
    with open(file_path, 'w', encoding='utf8') as file:
        file.write(content)

def process_directory(directory):
    # List all files in the given directory
    for filename in os.listdir(directory):
        if filename.endswith('.csv'):
            file_path = os.path.join(directory, filename)
            replace_characters(file_path)
            print(f"Processed {filename}")

# Example usage:
directory_path = '.'  # Set this to your directory path
process_directory(directory_path)
