"""Render website documentation into a generated GitHub wiki checkout."""
import re
import sys
from pathlib import Path

source = Path(sys.argv[1])
target = Path(sys.argv[2])
pages = sorted(source.glob('*.md'))
if not pages or not (source / 'getting-started.md').is_file():
    raise SystemExit('Expected documentation pages including getting-started.md')

names = {page.name: 'Home' if page.stem == 'getting-started' else page.stem for page in pages}
rendered = {}
for page in pages:
    text = page.read_text()
    # Convert local Markdown page links; preserve fragments and external links.
    def rewrite(match):
        destination = match.group(1)
        path, separator, fragment = destination.partition('#')
        name = names.get(path.removeprefix('./'))
        return '](' + (name + separator + fragment if name else destination) + ')'
    text = re.sub(r'\]\(([^)\s]+)\)', rewrite, text)
    rendered[names[page.name] + '.md'] = text

sidebar = ['<!-- Generated from website/src/docs; edit the source files. -->', '']
for page in sorted(pages, key=lambda p: (p.name != 'getting-started.md', p.name)):
    title = next(line[2:].strip() for line in page.read_text().splitlines() if line.startswith('# '))
    sidebar.append(f'- [{title}]({names[page.name]})')
rendered['_Sidebar.md'] = '\n'.join(sidebar) + '\n'

# This wiki is fully generated. Remove obsolete pages, retaining Git history.
target.mkdir(parents=True, exist_ok=True)
for page in target.glob('*.md'):
    if page.name not in rendered:
        page.unlink()
for name, text in rendered.items():
    (target / name).write_text(text)
