# Syntax Highlighting Test

This document tests the new syntax highlighting feature for code blocks.

## JSON Example

```json
{
  "name": "MarkMyWord",
  "version": "1.0.0",
  "description": "Markdown to Word converter",
  "count": 42,
  "enabled": true,
  "optional": null,
  "tags": ["markdown", "word", "converter"]
}
```

## TypeSpec Example

```typespec
namespace Pets;

model Pet {
  name: string;
  age: int32;
  species: PetSpecies;
}

enum PetSpecies {
  Dog,
  Cat,
  Bird
}

@route("/pets")
interface PetStore {
  @get
  list(): Pet[];

  @post
  create(@body pet: Pet): Pet;
}
```

## Plain Code Block (no language specified)

```
This is a plain code block without syntax highlighting.
It should render with monospace font but no colors.
```

## Bash Example

```bash
#!/bin/bash

# This is a comment
name="MarkMyWord"
count=42

if [ "$name" = "test" ]; then
    echo "Found: $name"
    exit 0
fi

for file in *.md; do
    echo "Processing: $file"
    ./convert.sh "$file"
done

# Function definition
function greet() {
    local name=$1
    echo "Hello, ${name}!"
}
```

## Unsupported Language

```python
# Python syntax highlighting is not yet implemented
def hello_world():
    print("Hello, World!")
    return 42
```
