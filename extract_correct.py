import json

file_path = r'C:\Users\Ulisses\.aws\[aws-profile-switcher]_fabiojunior11@gmail.com\amazonq\history\chat-history-7f9f1758ca7eb1c644d532ca63fbc4f0.json'

with open(file_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

# Extract titles and bodies recursively
def extract_messages(obj, results=[]):
    if isinstance(obj, dict):
        if 'title' in obj:
            results.append(('TITLE', obj['title']))
        if 'body' in obj:
            body = obj['body']
            if len(body) > 600:
                body = body[:600] + "..."
            results.append(('BODY', body))
        
        for value in obj.values():
            extract_messages(value, results)
    
    elif isinstance(obj, list):
        for item in obj:
            extract_messages(item, results)
    
    return results

messages = extract_messages(data, [])

print(f"Found {len(messages)} title/body entries\n")
print("="*80 + "\n")

# Show last 15 entries
for msg_type, content in messages[-15:]:
    print(f"[{msg_type}]")
    print(content)
    print("-"*80)
