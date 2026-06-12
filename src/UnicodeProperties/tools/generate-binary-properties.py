#!/usr/bin/env python3
# Generates compact Unicode binary-property range tables (Unicode 17.0.0) as a C#
# source file for the Broiler.Unicode UnicodeProperties library.
import sys

# ECMAScript-supported binary property: canonical name -> (source tag, [aliases])
BINARY = {
    "ASCII_Hex_Digit": ("PropList", ["AHex"]),
    "Bidi_Control": ("PropList", ["Bidi_C"]),
    "Dash": ("PropList", []),
    "Deprecated": ("PropList", ["Dep"]),
    "Diacritic": ("PropList", ["Dia"]),
    "Extender": ("PropList", ["Ext"]),
    "Hex_Digit": ("PropList", ["Hex"]),
    "IDS_Binary_Operator": ("PropList", ["IDSB"]),
    "IDS_Trinary_Operator": ("PropList", ["IDST"]),
    "Ideographic": ("PropList", ["Ideo"]),
    "Join_Control": ("PropList", ["Join_C"]),
    "Logical_Order_Exception": ("PropList", ["LOE"]),
    "Noncharacter_Code_Point": ("PropList", ["NChar"]),
    "Pattern_Syntax": ("PropList", ["Pat_Syn"]),
    "Pattern_White_Space": ("PropList", ["Pat_WS"]),
    "Quotation_Mark": ("PropList", ["QMark"]),
    "Radical": ("PropList", []),
    "Regional_Indicator": ("PropList", ["RI"]),
    "Sentence_Terminal": ("PropList", ["STerm"]),
    "Soft_Dotted": ("PropList", ["SD"]),
    "Terminal_Punctuation": ("PropList", ["Term"]),
    "Unified_Ideograph": ("PropList", ["UIdeo"]),
    "Variation_Selector": ("PropList", ["VS"]),
    "White_Space": ("PropList", ["space"]),
    "Math": ("DCP", []),
    "Alphabetic": ("DCP", ["Alpha"]),
    "Lowercase": ("DCP", ["Lower"]),
    "Uppercase": ("DCP", ["Upper"]),
    "Cased": ("DCP", []),
    "Case_Ignorable": ("DCP", ["CI"]),
    "Changes_When_Lowercased": ("DCP", ["CWL"]),
    "Changes_When_Uppercased": ("DCP", ["CWU"]),
    "Changes_When_Titlecased": ("DCP", ["CWT"]),
    "Changes_When_Casefolded": ("DCP", ["CWCF"]),
    "Changes_When_Casemapped": ("DCP", ["CWCM"]),
    "ID_Start": ("DCP", ["IDS"]),
    "ID_Continue": ("DCP", ["IDC"]),
    "XID_Start": ("DCP", ["XIDS"]),
    "XID_Continue": ("DCP", ["XIDC"]),
    "Default_Ignorable_Code_Point": ("DCP", ["DI"]),
    "Grapheme_Extend": ("DCP", ["Gr_Ext"]),
    "Grapheme_Base": ("DCP", ["Gr_Base"]),
    "Changes_When_NFKC_Casefolded": ("DNP", ["CWKCF"]),
    "Emoji": ("Emoji", []),
    "Emoji_Presentation": ("Emoji", ["EPres"]),
    "Emoji_Modifier": ("Emoji", ["EMod"]),
    "Emoji_Modifier_Base": ("Emoji", ["EBase"]),
    "Emoji_Component": ("Emoji", ["EComp"]),
    "Extended_Pictographic": ("Emoji", ["ExtPict"]),
}

def parse_prop_file(path):
    out = {}
    with open(path, encoding="utf-8") as f:
        for line in f:
            line = line.split("#", 1)[0].strip()
            if not line:
                continue
            parts = [p.strip() for p in line.split(";")]
            if len(parts) < 2:
                continue
            cps, prop = parts[0], parts[1]
            if ".." in cps:
                lo, hi = cps.split("..")
            else:
                lo = hi = cps
            out.setdefault(prop, []).append((int(lo, 16), int(hi, 16)))
    return out

def parse_unicodedata(path):
    rows = []
    with open(path, encoding="utf-8") as f:
        for line in f:
            line = line.rstrip("\n")
            if not line:
                continue
            fields = line.split(";")
            rows.append((int(fields[0], 16), fields[1], fields[9] == "Y"))
    assigned = []
    i = 0
    while i < len(rows):
        cp, name, _ = rows[i]
        if name.endswith(", First>"):
            cp2 = rows[i + 1][0]
            assigned.append((cp, cp2))
            i += 2
        else:
            assigned.append((cp, cp))
            i += 1
    bidim = [(cp, cp) for cp, _, m in rows if m]
    return normalize_ranges(assigned), normalize_ranges(bidim)

def normalize_ranges(ranges):
    ranges = sorted(ranges)
    merged = []
    for lo, hi in ranges:
        if merged and lo <= merged[-1][1] + 1:
            merged[-1] = (merged[-1][0], max(merged[-1][1], hi))
        else:
            merged.append((lo, hi))
    return merged

def encode(ranges):
    return ",".join(f"{lo:X}" if lo == hi else f"{lo:X}-{hi:X}" for lo, hi in ranges)

def norm_key(s):
    return s.replace("_", "").replace(" ", "").replace("-", "").lower()

def main():
    sources = {
        "PropList": parse_prop_file("PropList.txt"),
        "DCP": parse_prop_file("DerivedCoreProperties.txt"),
        "DNP": parse_prop_file("DerivedNormalizationProps.txt"),
        "Emoji": parse_prop_file("emoji-data.txt"),
    }
    assigned, bidim = parse_unicodedata("UnicodeData.txt")

    data = {}
    alias_map = {}
    for canon, (tag, aliases) in BINARY.items():
        src = sources[tag]
        if canon not in src:
            print(f"WARN: {canon} not in {tag}", file=sys.stderr)
            continue
        data[canon] = normalize_ranges(src[canon])
        alias_map[canon] = aliases

    data["Bidi_Mirrored"] = bidim;          alias_map["Bidi_Mirrored"] = ["Bidi_M"]
    data["Assigned"] = assigned;            alias_map["Assigned"] = []
    data["Any"] = [(0, 0x10FFFF)];          alias_map["Any"] = []
    data["ASCII"] = [(0, 0x7F)];            alias_map["ASCII"] = []

    entries = {}
    for canon, ranges in data.items():
        enc = encode(ranges)
        for k in [canon] + alias_map[canon]:
            entries[norm_key(k)] = enc

    lines = [
        "// <auto-generated>",
        "//   Generated from the Unicode Character Database 17.0.0 by ucdtmp/gen.py.",
        "//   Do not edit by hand. Maps an ECMAScript-normalized binary property name",
        "//   (canonical or alias) to a comma-separated list of code-point ranges",
        "//   (`lo` or `lo-hi`, hexadecimal).",
        "// </auto-generated>",
        "using System.Collections.Generic;",
        "",
        "namespace Broiler.Unicode.Properties;",
        "",
        "internal static class BinaryPropertyData",
        "{",
        "    internal static readonly Dictionary<string, string> Ranges = new()",
        "    {",
    ]
    for k in sorted(entries):
        lines.append(f'        ["{k}"] = "{entries[k]}",')
    lines += ["    };", "}"]
    with open("BinaryPropertyData.g.cs", "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")
    print(f"Emitted {len(entries)} keys, {len(data)} properties, {sum(len(v) for v in data.values())} ranges")

main()
