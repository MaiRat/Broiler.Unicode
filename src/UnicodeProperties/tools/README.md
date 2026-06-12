# UnicodeProperties data generation

`generate-binary-properties.py` regenerates
`../Generated/BinaryPropertyData.g.cs` from the Unicode Character Database.

```sh
# Download the UCD 17.0.0 data files next to the script, then run it:
base=https://www.unicode.org/Public/17.0.0/ucd
for f in PropList DerivedCoreProperties DerivedNormalizationProps UnicodeData; do
  curl -sO "$base/$f.txt"
done
curl -s -o emoji-data.txt "$base/emoji/emoji-data.txt"
python generate-binary-properties.py
mv BinaryPropertyData.g.cs ../Generated/
```

The output maps each ECMAScript-normalized binary property name (canonical name and
aliases, lower-cased with `_`/` `/`-` removed) to a comma-separated list of code-point
ranges (`lo` or `lo-hi`, hexadecimal). `Assigned` is derived from the assigned code
points in `UnicodeData.txt`; `Bidi_Mirrored` from its field-9 flag; `Any`/`ASCII` are
constant.
