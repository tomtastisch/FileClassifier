#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
OUT_DIR="${ROOT_DIR}/artifacts/audit"
mkdir -p "${OUT_DIR}"

INVENTORY_JSON="${OUT_DIR}/code_inventory.json"
CALLGRAPH_JSON="${OUT_DIR}/callgraph_inventory.json"
DEAD_JSON="${OUT_DIR}/dead_code_candidates.json"
REDUND_JSON="${OUT_DIR}/redundancy_candidates.json"
HARD_JSON="${OUT_DIR}/hardening_candidates.json"

# Inventory
python3 - "$ROOT_DIR" "$INVENTORY_JSON" <<'PY'
import datetime, hashlib, json, pathlib, sys
root=pathlib.Path(sys.argv[1])
out=pathlib.Path(sys.argv[2])
files=[]
for p in sorted((root/'src').rglob('*')):
    if p.suffix.lower() not in {'.vb','.cs'} or not p.is_file():
        continue
    if '/obj/' in p.as_posix() or '/bin/' in p.as_posix():
        continue
    rel=p.relative_to(root).as_posix()
    data=p.read_bytes()
    txt=data.decode('utf-8', errors='replace')
    files.append({
        'path': rel,
        'language': 'vb' if p.suffix.lower()=='.vb' else 'cs',
        'loc': len(txt.splitlines()),
        'sha256': hashlib.sha256(data).hexdigest(),
    })
generated_at=datetime.datetime.now(datetime.timezone.utc).replace(microsecond=0).isoformat().replace('+00:00','Z')
out.write_text(json.dumps({'generated_at': generated_at, 'files': files}, ensure_ascii=True), encoding='utf-8')
PY

python3 - "$ROOT_DIR" "$CALLGRAPH_JSON" "$DEAD_JSON" "$REDUND_JSON" "$HARD_JSON" <<'PY'
import json, pathlib, re, sys, collections, datetime
root=pathlib.Path(sys.argv[1])
callgraph_path=pathlib.Path(sys.argv[2])
dead_path=pathlib.Path(sys.argv[3])
redund_path=pathlib.Path(sys.argv[4])
hard_path=pathlib.Path(sys.argv[5])

decls=[]
vb_decl=re.compile(r'^\s*(Public|Private|Friend|Protected)?\s*(Shared\s+)?(Function|Sub)\s+([A-Za-z_][A-Za-z0-9_]*)')
cs_decl=re.compile(r'^\s*(public|private|internal|protected)\s+(?:static\s+|virtual\s+|override\s+|sealed\s+|async\s+|unsafe\s+|new\s+|partial\s+)*(?:[\w<>\[\],\.\?]+\s+)+([A-Za-z_][A-Za-z0-9_]*)\s*\(')

# Reference counts
source_texts={}
for p in list((root/'src').rglob('*.vb'))+list((root/'src').rglob('*.cs'))+list((root/'tests').rglob('*.cs')):
    if p.is_file():
        if '/obj/' in p.as_posix() or '/bin/' in p.as_posix():
            continue
        rel=p.relative_to(root).as_posix()
        txt=p.read_text(encoding='utf-8', errors='replace')
        source_texts[rel]=txt
        lines=txt.splitlines()
        if p.suffix.lower()=='.vb':
            for i,line in enumerate(lines, start=1):
                m=vb_decl.match(line)
                if m:
                    decls.append({'file':rel,'line':i,'symbol':m.group(4),'language':'vb'})
        elif p.suffix.lower()=='.cs':
            for i,line in enumerate(lines, start=1):
                m=cs_decl.match(line)
                if m:
                    decls.append({'file':rel,'line':i,'symbol':m.group(2),'language':'cs'})

ref_counts=[]
for d in decls:
    sym=d['symbol']
    pattern=re.compile(r'\b'+re.escape(sym)+r'\b')
    count=0
    for txt in source_texts.values():
        count += len(pattern.findall(txt))
    external_ref_count=max(count - 1, 0)
    ref_counts.append({**d,'reference_count':count,'reference_count_excluding_self':external_ref_count})

dead=[]
for r in ref_counts:
    if r['reference_count_excluding_self']==0 and '/src/' in ('/'+r['file']):
        dead.append({
            'type':'potential_dead_code',
            'file':r['file'],
            'line':r['line'],
            'symbol':r['symbol'],
            'evidence':'symbol has no detected references outside declaration in repository text search',
            'confidence':'low',
        })

# Redundancy heuristic: repeated line fragments in src
line_hits=collections.Counter()
line_locations=collections.defaultdict(list)
for fp,txt in source_texts.items():
    if not fp.startswith('src/'):
        continue
    for i,line in enumerate(txt.splitlines(), start=1):
        norm=line.strip()
        if len(norm)<40:
            continue
        if norm.startswith('Namespace ') or norm.startswith("'''") or 'LogGuard.' in norm:
            continue
        line_hits[norm]+=1
        if len(line_locations[norm])<5:
            line_locations[norm].append({'file':fp,'line':i})

redund=[]
for norm,count in line_hits.items():
    if count>=4:
        redund.append({
            'type':'potential_redundancy',
            'snippet':norm[:180],
            'occurrences':count,
            'sample_locations':line_locations[norm],
            'confidence':'low',
        })

# Hardening candidates: broad catch blocks in src
# Only classify explicit generic catch forms (`As Exception`) as broad.
hard=[]
broad_catch=re.compile(r'Catch\s+\w+\s+As\s+(?:Global\.System\.)?Exception\b')
for fp,txt in source_texts.items():
    if not fp.startswith('src/'):
        continue
    for i,line in enumerate(txt.splitlines(), start=1):
        if broad_catch.search(line):
            hard.append({
                'type':'broad_exception_catch',
                'file':fp,
                'line':i,
                'evidence':line.strip()[:220],
                'confidence':'medium',
            })

generated_at=datetime.datetime.now(datetime.timezone.utc).replace(microsecond=0).isoformat().replace('+00:00','Z')
callgraph_path.write_text(json.dumps({'generated_at':generated_at,'method_declarations':decls,'symbol_reference_counts':ref_counts,'edges':[],'notes':['heuristic baseline: declaration + repository symbol counts only']}, ensure_ascii=True), encoding='utf-8')
dead_path.write_text(json.dumps({'generated_at':generated_at,'candidates':dead}, ensure_ascii=True), encoding='utf-8')
redund_path.write_text(json.dumps({'generated_at':generated_at,'candidates':redund}, ensure_ascii=True), encoding='utf-8')
hard_path.write_text(json.dumps({'generated_at':generated_at,'candidates':hard}, ensure_ascii=True), encoding='utf-8')
PY

echo "Generated JSON artifacts in ${OUT_DIR}" >&2
