# -*- coding: utf-8 -*-
"""VARCO get_workflow_graph 결과(저장된 txt)에서 노드 상태 요약.
사용: python vstat.py <graph.txt> [id-prefix ...]   (prefix 없으면 상태별 개수)"""
import json, sys

g = json.load(open(sys.argv[1], encoding='utf-8'))['graph']
nodes = g['nodes']
pre = sys.argv[2:]
if not pre:
    from collections import Counter
    print(Counter((n['type'], n.get('status')) for n in nodes))
    xs = [n['position'] for n in nodes]
    print('max y', max(p['y'] for p in xs), 'min x', min(p['x'] for p in xs), 'max x', max(p['x'] for p in xs))
for p in pre:
    for n in nodes:
        if n['id'].startswith(p):
            outs = []
            for o in n.get('output') or []:
                v=o.get('value') or {}
                outs.append(v.get('url') or v.get('taskId') or str(o)[:80])
            print(n['id'][:8], n['type'], n.get('status'), n.get('progress', ''), outs[:4])
