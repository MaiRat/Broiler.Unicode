# UnicodeEmoji.StringProperties

Managed, allocation-light matching of **Unicode RGI emoji string properties** (UTS #51) for .NET,
backed by a generated trie. No ICU, no native dependency, no runtime downloads, no giant regex.

- **Supported Unicode emoji version:** **16.0**
- **Target frameworks:** `net8.0`, `net10.0`
- **Dependencies:** none (pure managed C#)

```csharp
using UnicodeEmoji.StringProperties;

EmojiStringProperties.IsRgiEmoji("👨‍👩‍👧‍👦");   // true  (family ZWJ sequence)
EmojiStringProperties.IsBasicEmoji("❤️");         // true  (U+2764 U+FE0F)
EmojiStringProperties.IsBasicEmoji("❤");          // false (missing U+FE0F presentation selector)
EmojiStringProperties.IsEmojiZwjSequence("👩🏽‍💻"); // true  (woman technologist, medium skin tone)
```

## Why a string property and not a code point property?

Emoji-ness is **not** a property of a single code point. The "woman technologist, medium skin tone"
emoji `👩🏽‍💻` is the sequence `U+1F469 U+1F3FD U+200D U+1F4BB` — four scalar values, six UTF-16
`char`s. Whether something is an emoji depends on the *whole sequence*: the presence of U+FE0F, the
zero-width joiners, regional-indicator pairs, tag characters, skin-tone modifiers, and so on.

`RGI_Emoji` (**R**ecommended **f**or **G**eneral **I**nterchange) is defined by UTS #51 as the union of
several **string** properties:

| Property                       | Example            | Shape                                        |
|--------------------------------|--------------------|----------------------------------------------|
| `Basic_Emoji`                  | `😀` `❤️`          | one emoji code point, optionally `+ U+FE0F`  |
| `Emoji_Keycap_Sequence`        | `1️⃣`              | `digit U+FE0F U+20E3`                         |
| `RGI_Emoji_Flag_Sequence`      | `🇩🇪`              | a pair of regional indicator symbols         |
| `RGI_Emoji_Tag_Sequence`       | `🏴󠁧󠁢󠁥󠁮󠁧󠁿`     | black flag `+` tag characters `+ U+E007F`     |
| `RGI_Emoji_Modifier_Sequence`  | `👋🏽`              | base emoji `+` skin-tone modifier            |
| `RGI_Emoji_ZWJ_Sequence`       | `👨‍👩‍👧‍👦` `🧑‍🤝‍🧑` | emoji joined with `U+200D`                |

```
RGI_Emoji = Basic_Emoji
          ∪ Emoji_Keycap_Sequence
          ∪ RGI_Emoji_Flag_Sequence
          ∪ RGI_Emoji_Tag_Sequence
          ∪ RGI_Emoji_Modifier_Sequence
          ∪ RGI_Emoji_ZWJ_Sequence
```

This library matches on **Unicode scalar values**, decoding surrogate pairs first, so it never assumes
one UTF-16 `char` equals one character.

## Installation

```sh
dotnet add package UnicodeEmoji.StringProperties
```

## API

All exact-match APIs require the **entire** input to be a single, fully-qualified emoji. They come in
`string` and `ReadOnlySpan<char>` overloads.

```csharp
// Exact whole-string predicates
bool  EmojiStringProperties.IsRgiEmoji(string value);
bool  EmojiStringProperties.IsRgiEmoji(ReadOnlySpan<char> value);
bool  EmojiStringProperties.IsBasicEmoji(/* string | ReadOnlySpan<char> */);
bool  EmojiStringProperties.IsEmojiZwjSequence(/* string | ReadOnlySpan<char> */);

// Full property set for a single sequence (None if not an exact RGI emoji)
EmojiSequenceProperties EmojiStringProperties.GetProperties(/* string | ReadOnlySpan<char> */);

// Test for a specific combination of properties
bool  EmojiStringProperties.HasProperties(ReadOnlySpan<char> value, EmojiSequenceProperties mask);

// Allocation-free enumeration of emoji embedded in text (greedy / leftmost-longest)
EmojiSequenceEnumerator EmojiStringProperties.EnumerateRgiEmoji(ReadOnlySpan<char> text);
EmojiSequenceEnumerator EmojiStringProperties.Enumerate(ReadOnlySpan<char> text, EmojiSequenceProperties filter);

// The Unicode emoji version the tables were generated from
string EmojiStringProperties.UnicodeEmojiVersion; // "16.0"
```

### Enumeration without allocating strings

`EnumerateRgiEmoji` returns a `ref struct` enumerator that yields `EmojiMatch` values describing each
emoji by **index and length** (in UTF-16 code units). No substrings are allocated — slice the original
text yourself if you need the characters.

```csharp
ReadOnlySpan<char> text = "Hi 👋 family 👨‍👩‍👧‍👦 from 🇩🇪!";

foreach (EmojiMatch m in EmojiStringProperties.EnumerateRgiEmoji(text))
{
    ReadOnlySpan<char> emoji = text.Slice(m.Index, m.Length);
    Console.WriteLine($"{m.Index}..{m.End} -> {m.Properties}");
}
```

### Classifying a single value

```csharp
EmojiSequenceProperties p = EmojiStringProperties.GetProperties("🇩🇪");
// p == EmojiSequenceProperties.RgiEmojiFlagSequence
//   (and p has the RgiEmoji bits, since every sequence property is a subset of RGI_Emoji)
```

## How it works

The official Unicode data files are treated as the **source of truth** and are committed under
[`data/unicode/16.0/`](data/unicode/16.0):

- `emoji-test.txt`
- `emoji-sequences.txt`
- `emoji-zwj-sequences.txt`

A build-time generator parses those files, normalizes every entry into an array of Unicode scalar
values, classifies it into one of the six properties above, and builds a **flattened trie** keyed on
scalar values. The trie is emitted as compact, diff-friendly C# arrays
([`EmojiTrieData.g.cs`](src/UnicodeEmoji.StringProperties/Generated/EmojiTrieData.g.cs)) that are
**committed to source control**. At runtime the library only walks those arrays — it never parses data
files, uses reflection, or emits code.

Matching uses leftmost-longest semantics so the longest valid emoji (e.g. the complete four-person
family) wins over its shorter prefixes, and exact-match APIs require the entire input to terminate on a
trie node, so partial sequences are never reported as RGI.

## Updating to a new Unicode version

The `DataTool` downloads the official files and regenerates the lookup tables:

```sh
# Download data/unicode/<version>/ and regenerate src/.../Generated/EmojiTrieData.g.cs
dotnet run --project src/UnicodeEmoji.StringProperties.DataTool -- update 16.0

# Or as separate steps:
dotnet run --project src/UnicodeEmoji.StringProperties.DataTool -- download 16.0
dotnet run --project src/UnicodeEmoji.StringProperties.DataTool -- generate 16.0
```

Then bump `UnicodeEmojiVersion` in [`Directory.Build.props`](Directory.Build.props), review the diff of
the generated file, run the tests, and commit. Data is fetched **only** by this tool — never during a
normal build or at runtime.

## Benchmarks

[`benchmarks/`](benchmarks) compares the generated trie against a naive `HashSet<string>` and a single
large compiled `Regex`, for both exact matching and enumeration:

```sh
dotnet run -c Release --project benchmarks/UnicodeEmoji.StringProperties.Benchmarks -- --filter '*'
```

The trie matches in time proportional to the input length (independent of the ~3,800-entry data set),
allocates nothing on the hot path, and avoids the large startup and memory cost of compiling a
giant alternation regex.

## Limitations

- **RGI only.** This library implements the *recommended-for-general-interchange* set. It does **not**
  recognize valid-but-not-recommended emoji ZWJ sequences, arbitrary tag sequences, or every
  emoji-presentation code point in isolation.
- **Exact match means fully-qualified.** Minimally-qualified / unqualified forms (those missing a
  required U+FE0F) are intentionally **not** reported as RGI by the exact-match APIs.
- **Not a grapheme segmenter.** `EnumerateRgiEmoji` finds RGI emoji; it is not a general Unicode
  text-segmentation (UAX #29) implementation.
- **Emoji presentation is matching-only.** The library reports properties of text; it does not render,
  normalize, or transform emoji.
- **Version-pinned.** The data is generated for a single Unicode version (currently 16.0). Use the
  `DataTool` to move to another version.

## Project layout

```
src/
  UnicodeEmoji.StringProperties/            # the NuGet library (net8.0; net10.0)
  UnicodeEmoji.StringProperties.Generator/  # parser + trie builder + source emitter
  UnicodeEmoji.StringProperties.DataTool/   # downloads data + drives the generator
tests/
  UnicodeEmoji.StringProperties.Tests/      # golden + data-driven xUnit tests
benchmarks/
  UnicodeEmoji.StringProperties.Benchmarks/ # trie vs HashSet vs Regex
data/
  unicode/16.0/                             # committed official Unicode data files
```

## License

MIT. Unicode data files are © Unicode, Inc. and distributed under the
[Unicode Terms of Use](https://www.unicode.org/terms_of_use.html).
