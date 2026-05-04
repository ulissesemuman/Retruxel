with open(r'F:\Junior\Desenvolvimento de Jogos\Ports\Master System\history\chat-history-7f9f1758ca7eb1c644d532ca63fbc4f0.json', 'r', encoding='utf-8') as f:
    lines = f.readlines()

print(f"Total lines: {len(lines)}\n")

# Start from line 3000
start_line = 3000
relevant_lines = []

for i in range(start_line, len(lines)):
    line = lines[i].strip()
    
    if '"title":' in line or '"body":' in line:
        # Clean up the line
        line = line.replace('\\n', '\n').replace('\\"', '"').replace('\\\\', '\\')
        relevant_lines.append((i+1, line))  # +1 for 1-based line numbers

print(f"Found {len(relevant_lines)} relevant lines from line {start_line} onwards\n")
print("="*80 + "\n")

for line_num, content in relevant_lines:
    print(f"[Line {line_num}]")
    # Limit output to 500 chars per line
    if len(content) > 500:
        print(content[:500] + "...")
    else:
        print(content)
    print("-"*80)
