import { useEffect, useMemo, useRef, useState } from 'react';
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
  const [profile, setProfile] = useState<FamilyMember | null>(null);
  const [draggedId, setDraggedId] = useState('');
  const pointerDrag = useRef<{ id: string; x: number; y: number; moved: boolean } | null>(null);
  const suppressClick = useRef(false);
  useEffect(() => {
    if (!canEdit) return;
    const onMove = (event: PointerEvent) => {
      const drag = pointerDrag.current;
      if (!drag) return;
      if (Math.hypot(event.clientX - drag.x, event.clientY - drag.y) > 8) {
        drag.moved = true;
        setDraggedId(drag.id);
      }
    };
    const onUp = (event: PointerEvent) => {
      const drag = pointerDrag.current;
      if (!drag) return;
      pointerDrag.current = null;
      if (!drag.moved) { setDraggedId(''); return; }
      suppressClick.current = true;
      window.setTimeout(() => { suppressClick.current = false; }, 0);
      const target = document.elementFromPoint(event.clientX, event.clientY)?.closest<HTMLElement>('[data-family-member-id]');
      const targetId = target?.dataset.familyMemberId;
      if (targetId && targetId !== drag.id) {
        setParentId(drag.id);
        setChildId(targetId);
        setKind('ParentChild');
        setError('');
        setShowForm(true);
      }
      setDraggedId('');
    };
    document.addEventListener('pointermove', onMove);
    document.addEventListener('pointerup', onUp);
    document.addEventListener('pointercancel', onUp);
    return () => {
      document.removeEventListener('pointermove', onMove);
      document.removeEventListener('pointerup', onUp);
      document.removeEventListener('pointercancel', onUp);
    };
  }, [canEdit]);
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

  const profileRelations = profile ? (() => {
    const rows: Array<{ label: string; member: FamilyMember }> = [];
    const add = (label: string, id: string) => {
      const member = memberMap.get(id);
      if (member && member.familyMemberId !== profile.familyMemberId && !rows.some(x => x.label === label && x.member.familyMemberId === id)) rows.push({ label, member });
    };
    const parentIds = relationships.filter(r => r.relationshipType === 'ParentChild' && r.toFamilyMemberId === profile.familyMemberId).map(r => r.fromFamilyMemberId);
    parentIds.forEach(id => add('Parent', id));
    relationships.filter(r => r.relationshipType === 'ParentChild' && r.fromFamilyMemberId === profile.familyMemberId).forEach(r => add('Child', r.toFamilyMemberId));
    relationships.filter(r => r.relationshipType === 'Spouse' && (r.fromFamilyMemberId === profile.familyMemberId || r.toFamilyMemberId === profile.familyMemberId)).forEach(r => add('Spouse', r.fromFamilyMemberId === profile.familyMemberId ? r.toFamilyMemberId : r.fromFamilyMemberId));
    relationships.filter(r => r.relationshipType === 'ParentChild' && parentIds.includes(r.fromFamilyMemberId) && r.toFamilyMemberId !== profile.familyMemberId).forEach(r => add('Sibling', r.toFamilyMemberId));
    return rows;
  })() : [];
  const personButton = (m: FamilyMember, subtitle: string) => <div
    role="button"
    tabIndex={0}
    data-family-member-id={m.familyMemberId}
    className={`tree-person tree-person-button ${canEdit?'tree-draggable':''} ${draggedId===m.familyMemberId?'tree-dragging':''}`}
    key={m.familyMemberId}
    onClick={() => { if (!suppressClick.current) setProfile(m); }}
    onKeyDown={e=>{if(e.key==='Enter'||e.key===' '){e.preventDefault();setProfile(m);}}}
    onPointerDown={e=>{if(canEdit&&e.button===0){pointerDrag.current={id:m.familyMemberId,x:e.clientX,y:e.clientY,moved:false};}}}
    aria-label={canEdit?`Drag ${m.fullName} onto another member to create a relationship, or click to view profile`:`View ${m.fullName} profile`}
  >
    <span className="tree-avatar"><Users size={18}/></span><strong>{name(m)}</strong><small>{canEdit?"Drag onto another person":subtitle}</small>
  </div>;
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
    <div className="tree-toolbar"><div><span className="eyebrow">FAMILY RELATIONSHIPS</span><p>{relationships.length} saved relationships · {canEdit?'Drag one person onto another to connect them':'siblings are derived from shared parents'}</p></div>{canEdit&&<button className="primary-btn" onClick={()=>setShowForm(true)}><Plus size={16}/>Add relationship</button>}</div>
    {error&&<div className="alert">{error}</div>}
    {parentGroups.map(group=><div className="tree-family" key={group.parent.familyMemberId}>
      {personButton(group.parent, 'Parent · View profile')}
      <div className="tree-stem"><span/></div>
      <div className="tree-children">{group.children.map(child=><div className="tree-child-wrap" key={child.familyMemberId}>{personButton(child, 'Child' + (group.children.length>1?' · Sibling group':'') + ' · View profile')}</div>)}</div>
      {group.children.length>1&&<div className="sibling-note"><GitBranch size={14}/> {group.children.length} siblings through a shared parent</div>}
      {canEdit&&relationships.filter(r=>r.relationshipType==='ParentChild'&&r.fromFamilyMemberId===group.parent.familyMemberId).map(r=><button className="relationship-remove" key={r.familyRelationshipId} disabled={busy} onClick={()=>remove(r.familyRelationshipId)}><Trash2 size={13}/>Remove {r.toFullName}</button>)}
    </div>)}
    {spouses.length>0&&<div className="spouse-list"><h3>Spouse relationships</h3>{spouses.map(r=><div className="spouse-row" key={r.familyRelationshipId}><button type="button" className="spouse-person-link" onClick={()=>{const m=memberMap.get(r.fromFamilyMemberId);if(m)setProfile(m);}}>{r.fromFullName}</button><span className="spouse-heart">♥</span><button type="button" className="spouse-person-link" onClick={()=>{const m=memberMap.get(r.toFamilyMemberId);if(m)setProfile(m);}}>{r.toFullName}</button>{canEdit&&<button onClick={()=>remove(r.familyRelationshipId)} disabled={busy} aria-label="Remove spouse relationship"><Trash2 size={14}/></button>}</div>)}</div>}
    {isolated.length>0&&<div className="tree-unlinked"><h3>Other family members</h3><div>{isolated.map(m=>personButton(m, m.status + ' · View profile'))}</div></div>}
    {relationships.length===0&&<div className="tree-empty"><GitBranch size={30}/><strong>No relationships added yet</strong><span>Add parent-child or spouse links to build the family tree.</span></div>}
    {profile&&<div className="modal-backdrop" onClick={()=>setProfile(null)}><div className="modal profile-modal" onClick={e=>e.stopPropagation()}><button className="modal-close" onClick={()=>setProfile(null)}><X size={16}/></button><span className="eyebrow">FAMILY MEMBER PROFILE</span><div className="profile-heading"><span className="tree-avatar"><Users size={22}/></span><div><h2>{profile.fullName}</h2>{profile.tamilName&&<p>{profile.tamilName}</p>}</div></div><div className="profile-facts">{profile.gender&&<div><small>Gender</small><strong>{profile.gender}</strong></div>}{profile.dateOfBirth&&<div><small>Date of birth</small><strong>{new Date(profile.dateOfBirth+'T00:00:00').toLocaleDateString()}</strong></div>}{profile.dateOfDeath&&<div><small>Date of death</small><strong>{new Date(profile.dateOfDeath+'T00:00:00').toLocaleDateString()}</strong></div>}{profile.placeOfBirth&&<div><small>Place of birth</small><strong>{profile.placeOfBirth}</strong></div>}{profile.contactNumber&&<div><small>Contact</small><strong>{profile.contactNumber}</strong></div>}{profile.email&&<div><small>Email</small><strong>{profile.email}</strong></div>}<div><small>Status</small><strong>{profile.status}</strong></div></div><div className="profile-relatives"><h3>Family relationships</h3>{profileRelations.length===0?<p className="muted">No linked relationships recorded for this person.</p>:profileRelations.map((r,i)=><div className="profile-relative" key={r.label+'-'+r.member.familyMemberId+'-'+i}><span>{r.label}</span><strong>{r.member.fullName}</strong>{r.member.tamilName&&<small>{r.member.tamilName}</small>}</div>)}</div><div className="modal-actions"><button className="secondary-btn" onClick={()=>setProfile(null)}>Close</button></div></div></div>}
    {showForm&&<div className="modal-backdrop" onClick={()=>setShowForm(false)}><div className="modal" onClick={e=>e.stopPropagation()}><button className="modal-close" onClick={()=>setShowForm(false)}><X size={16}/></button><span className="eyebrow">FAMILY RELATIONSHIP</span><h2>Add relationship</h2><label>Relationship type<select value={kind} onChange={e=>setKind(e.target.value as 'ParentChild'|'Spouse')}><option value="ParentChild">Dragged person is Parent → dropped person is Child</option><option value="Spouse">Spouse (either direction)</option></select></label><label>{kind==='ParentChild'?'Parent':'First spouse'}<select value={parentId} onChange={e=>setParentId(e.target.value)}><option value="">Select member</option>{activeMembers.map(m=><option key={m.familyMemberId} value={m.familyMemberId}>{m.fullName}</option>)}</select></label><label>{kind==='ParentChild'?'Child':'Second spouse'}<select value={childId} onChange={e=>setChildId(e.target.value)}><option value="">Select member</option>{activeMembers.filter(m=>m.familyMemberId!==parentId).map(m=><option key={m.familyMemberId} value={m.familyMemberId}>{m.fullName}</option>)}</select></label><div className="modal-actions"><button className="secondary-btn" onClick={()=>setShowForm(false)}>Cancel</button><button className="primary-btn" disabled={busy||!parentId||!childId} onClick={submit}>Save relationship</button></div></div></div>}
  </section>;
}
