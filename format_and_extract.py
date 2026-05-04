import json

with open(r'F:\Junior\Desenvolvimento de Jogos\Ports\Master System\history\chat-history-7f9f1758ca7eb1c644d532ca63fbc4f0.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

# Pretty print to file with line breaks
with open(r'F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\formatted.json', 'w', encoding='utf-8') as f:
    json.dump(data, f, indent=2, ensure_ascii=False)

print("Formatted JSON saved to formatted.json")

# Now extract titles and bodies
def extract_messages(obj, path=""):
    results = []
    
    if isinstance(obj, dict):
        if 'title' in obj:
            results.append(('title', obj['title']))
        if 'body' in obj:
            body = obj['body']
            if len(body) > 500:
                body = body[:500] + "..."
            results.append(('body', body))
        
        for key, value in obj.items():
            results.extend(extract_messages(value, f"{path}.{key}"))
    
    elif isinstance(obj, list):
        for i, item in enumerate(obj):
            results.extend(extract_messages(item, f"{path}[{i}]"))
    
    return results

messages = extract_messages(data)

print(f"\nFound {len(messages)} title/body entries:\n")
print("="*80 + "\n")

for msg_type, content in messages[-20:]:  # Show last 20
    print(f"[{msg_type.upper()}]")
    print(content)
    print("-"*80)
