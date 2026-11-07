import { useMemo, useState } from 'react';
import { GitBranch, Plus, Trash2, Users, X } from 'lucide-react';
import { api, FamilyMember, FamilyRelationship } from './api';

type Props = {
  familyId: string;
  members: FamilyMember[];
  relationships: FamilyRelationship[];
  canEdit: boolean;
  onChanged: () => Promise<void>;
};

export default function FamilyTree({ familyId, members, relationships, canEdit, onChanged }: Props) {
  const [showForm, setShowForm] = useState(false);
  const [parentId, setParentId] = useState('');
  const [childId, setChildId] = useState('');
  const [kind, setKind] = useState<'ParentChild'|'Spouse'>('ParentChild');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const activeMembers = members.filter(m => m.status !== 'Archived');
  const memberMap = useMemo(() => new Map(activeMembers.map(m => [m.familyMemberId,m])), [activeMembers]);
  const parentGroups = useMemo(() => {
    const map = new Map<string, FamilyMember[]>();
    relationships.filter(r => r.relationshipType === 'ParentChild').forEach(r => {
      const parent = memberMap.get(r.fromFamilyMemberId);
      const child = memberMap.get(r.toFamilyMemberId);
      if (parent && child) map.set(parent.familyMemberId,[...(map.get(parent.familyMemberId)||[]),child]);
    });
    return [...map.entries()].map(([id,children]) => ({ parent: memberMap.get(id)!, children }));
  }, [relationships,memberMap]);
  const spouses = relationships.filter(r => r.relationshipType === 'Spouse');
  const connectedIds = new Set(relationships.flatMap(r => [r.fromFamilyMemberId,r.toFamilyMemberId]));
  const isolated = activeMembers.filter(m => !connectedIds.has(m.familyMemberId));
  const siblingsExist = parentGroups.some(g => g.children.length > 1);
  const submit = async () => {
    if (!parentId || !childId || parentId === childId) return;
    setBusy(true); setError('');
    try { await api.createRelationship(familyId,parentId,childId,kind); setShowForm(false); setParentId(''); setChildId(''); await onChanged(); }
    catch(e) { setError(e instanceof Error ? e.message : 'Unable to save relationship'); }
    finally { setBusy(false); }
  };
  const remove = async (id:string) => {
    if (!window.confirm('Delete this relationship?')) return;
    setBusy(true); setError('');
    try { await api.deleteRelationship(familyId,id); await onChanged(); }
    catch(e) { setError(e instanceof Error ? e.message : 'Unable to delete relationship'); }
    finally { setBusy(false); }
  };
  const name = (m:FamilyMember) => <>{m.fullName}{m.tamilName && <small>{m.tamilName}</small>}</>;
  return <section className="tree-panel">
    <div className="tree-toolbar"><div><span className="eyebrow">FAMILY RELATIONSHIPS</span><p>{relationships.length} saved relationships · siblings are derived from shared parents</p></div>{canEdit&&<button className="primary-btn" onClick={()=>setShowForm(true)}><Plus size={16}/>Add relationship</button>}</div>
    {error&&<div className="alert">{error}</div>}
    {parentGroups.map(group=><div className="tree-family" key={group.parent.familyMemberId}>
      <div className="tree-person parent-person"><span className="tree-avatar"><Users size={19}/></span><strong>{name(group.parent)}</strong><small>Parent</small></div>
      <div className="tree-stem"><span/></div>
      <div className="tree-children">{group.children.map(child=><div className="tree-child-wrap" key={child.familyMemberId}><div className="tree-person"><span className="tree-avatar"><Users size={18}/></span><strong>{name(child)}</strong><small>Child{group.children.length>1?' · Sibling group':''}</small></div></div>)}</div>
      {group.children.length>1&&<div className="sibling-note"><GitBranch size={14}/> {group.children.length} siblings through a shared parent</div>}
      {canEdit&&relationships.filter(r=>r.relationshipType==='ParentChild'&&r.fromFamilyMemberId===group.parent.familyMemberId).map(r=><button className="relationship-remove" key={r.familyRelationshipId} disabled={busy} onClick={()=>remove(r.familyRelationshipId)}><Trash2 size={13}/>Remove {r.toFullName}</button>)}
    </div>)}
    {spouses.length>0&&<div className="spouse-list"><h3>Spouse relationships</h3>{spouses.map(r=><div className="spouse-row" key={r.familyRelationshipId}><span>{r.fromFullName}</span><span className="spouse-heart">♥</span><span>{r.toFullName}</span>{canEdit&&<button onClick={()=>remove(r.familyRelationshipId)} disabled={busy} aria-label="Remove spouse relationship"><Trash2 size={14}/></button>}</div>)}</div>}
    {isolated.length>0&&<div className="tree-unlinked"><h3>Other family members</h3><div>{isolated.map(m=><div className="tree-person" key={m.familyMemberId}><span className="tree-avatar"><Users size={18}/></span><strong>{name(m)}</strong><small>{m.status}</small></div>)}</div></div>}
    {relationships.length===0&&<div className="tree-empty"><GitBranch size={30}/><strong>No relationships added yet</strong><span>Add parent-child or spouse links to build the family tree.</span></div>}
    {showForm&&<div className="modal-backdrop" onClick={()=>setShowForm(false)}><div className="modal" onClick={e=>e.stopPropagation()}><button className="modal-close" onClick={()=>setShowForm(false)}><X size={16}/></button><span className="eyebrow">FAMILY RELATIONSHIP</span><h2>Add relationship</h2><label>Relationship type<select value={kind} onChange={e=>setKind(e.target.value as 'ParentChild'|'Spouse')}><option value="ParentChild">Parent → Child</option><option value="Spouse">Spouse</option></select></label><label>{kind==='ParentChild'?'Parent':'First spouse'}<select value={parentId} onChange={e=>setParentId(e.target.value)}><option value="">Select member</option>{activeMembers.map(m=><option key={m.familyMemberId} value={m.familyMemberId}>{m.fullName}</option>)}</select></label><label>{kind==='ParentChild'?'Child':'Second spouse'}<select value={childId} onChange={e=>setChildId(e.target.value)}><option value="">Select member</option>{activeMembers.filter(m=>m.familyMemberId!==parentId).map(m=><option key={m.familyMemberId} value={m.familyMemberId}>{m.fullName}</option>)}</select></label><div className="modal-actions"><button className="secondary-btn" onClick={()=>setShowForm(false)}>Cancel</button><button className="primary-btn" disabled={busy||!parentId||!childId} onClick={submit}>Save relationship</button></div></div></div>}
  </section>;
}
