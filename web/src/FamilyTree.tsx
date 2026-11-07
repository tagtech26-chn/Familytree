import { useMemo, useRef, useState } from 'react';
import { GitBranch, Link2, Move, Plus, Trash2, Users, X } from 'lucide-react';
import { api, FamilyMember, FamilyRelationship } from './api';

type Props = { familyId: string; members: FamilyMember[]; relationships: FamilyRelationship[]; canEdit: boolean; onChanged: () => Promise<void> };
type Point = { x: number; y: number };
type DragState = { id: string; offsetX: number; offsetY: number } | null;

export default function FamilyTree({ familyId, members, relationships, canEdit, onChanged }: Props) {
  const active = members.filter(m => m.status !== 'Archived');
  const memberMap = useMemo(() => new Map(active.map(m => [m.familyMemberId, m])), [active]);
  const [positions, setPositions] = useState<Record<string, Point>>({});
  const [drag, setDrag] = useState<DragState>(null);
  const workspaceRef = useRef<HTMLDivElement>(null);
  const [connectFrom, setConnectFrom] = useState('');
  const [targetId, setTargetId] = useState('');
  const [kind, setKind] = useState<'ParentChild' | 'Spouse'>('ParentChild');
  const [showForm, setShowForm] = useState(false);
  const [profile, setProfile] = useState<FamilyMember | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const pointFor = (id: string, index: number): Point => positions[id] || ({ x: 28 + (index % 4) * 230, y: 28 + Math.floor(index / 4) * 165 });
  const positionIndex = (id: string) => active.findIndex(m => m.familyMemberId === id);
  const point = (id: string) => pointFor(id, positionIndex(id));
  const relatives = useMemo(() => {
    if (!profile) return [];
    const rows: Array<{ label: string; member: FamilyMember }> = [];
    const add = (label: string, id: string) => { const m = memberMap.get(id); if (m && m.familyMemberId !== profile.familyMemberId && !rows.some(x => x.label === label && x.member.familyMemberId === id)) rows.push({ label, member: m }); };
    const parents = relationships.filter(r => r.relationshipType === 'ParentChild' && r.toFamilyMemberId === profile.familyMemberId).map(r => r.fromFamilyMemberId);
    parents.forEach(id => add('Parent', id));
    relationships.filter(r => r.relationshipType === 'ParentChild' && r.fromFamilyMemberId === profile.familyMemberId).forEach(r => add('Child', r.toFamilyMemberId));
    relationships.filter(r => r.relationshipType === 'Spouse' && (r.fromFamilyMemberId === profile.familyMemberId || r.toFamilyMemberId === profile.familyMemberId)).forEach(r => add('Spouse', r.fromFamilyMemberId === profile.familyMemberId ? r.toFamilyMemberId : r.fromFamilyMemberId));
    relationships.filter(r => r.relationshipType === 'ParentChild' && parents.includes(r.fromFamilyMemberId) && r.toFamilyMemberId !== profile.familyMemberId).forEach(r => add('Sibling', r.toFamilyMemberId));
    return rows;
  }, [profile, relationships, memberMap]);

  const startMove = (event: React.PointerEvent<HTMLDivElement>, id: string) => {
    if (!canEdit || (event.target as HTMLElement).closest('button')) return;
    const rect = workspaceRef.current?.getBoundingClientRect();
    if (!rect) return;
    const p = point(id);
    setDrag({ id, offsetX: event.clientX - rect.left - p.x, offsetY: event.clientY - rect.top - p.y });
    event.currentTarget.setPointerCapture(event.pointerId);
  };
  const moveCard = (event: React.PointerEvent<HTMLDivElement>) => {
    if (!drag) return;
    const rect = workspaceRef.current?.getBoundingClientRect();
    if (!rect) return;
    const x = Math.max(8, event.clientX - rect.left - drag.offsetX);
    const y = Math.max(8, event.clientY - rect.top - drag.offsetY);
    setPositions(old => ({ ...old, [drag.id]: { x, y } }));
  };
  const finishMove = () => setDrag(null);
  const chooseConnectionTarget = (id: string) => {
    if (!connectFrom) { setConnectFrom(id); return; }
    if (connectFrom === id) { setConnectFrom(''); return; }
    setTargetId(id); setKind('ParentChild'); setShowForm(true); setError('');
  };
  const save = async () => {
    if (!connectFrom || !targetId || connectFrom === targetId) return;
    setBusy(true); setError('');
    try {
      await api.createRelationship(familyId, connectFrom, targetId, kind);
      setShowForm(false); setConnectFrom(''); setTargetId('');
      await onChanged();
    } catch (e) { setError(e instanceof Error ? e.message : 'Unable to save relationship'); }
    finally { setBusy(false); }
  };
  const remove = async (id: string) => {
    if (!window.confirm('Delete this relationship?')) return;
    setBusy(true); setError('');
    try { await api.deleteRelationship(familyId, id); await onChanged(); }
    catch (e) { setError(e instanceof Error ? e.message : 'Unable to delete relationship'); }
    finally { setBusy(false); }
  };
  const cardCenter = (id: string) => { const p = point(id); return { x: p.x + 92, y: p.y + 48 }; };

  return <section className="tree-panel visual-editor">
    <div className="tree-toolbar">
      <div><span className="eyebrow">FAMILY RELATIONSHIP EDITOR</span><p>{relationships.length} saved relationships · {canEdit ? 'Move cards freely, then select Connect on a person and choose another person.' : 'Select a person to view their profile and relationships.'}</p></div>
      {canEdit && <div className="editor-actions"><button className="secondary-btn" onClick={() => { setConnectFrom(''); setTargetId(''); setKind('ParentChild'); setShowForm(true); }}><Plus size={15}/> Add relationship</button><span className="editor-hint"><Move size={14}/> Drag cards to arrange</span></div>}
    </div>
    {error && <div className="alert">{error}</div>}
    {connectFrom && <div className="connection-banner">Choose the second person to connect to <strong>{memberMap.get(connectFrom)?.fullName}</strong>. <button onClick={() => setConnectFrom('')}>Cancel</button></div>}
    <div className="relationship-workspace" ref={workspaceRef} onPointerMove={moveCard} onPointerUp={finishMove} onPointerCancel={finishMove}>
      <svg className="relationship-lines" aria-hidden="true">
        {relationships.map(r => {
          const a = memberMap.has(r.fromFamilyMemberId) ? cardCenter(r.fromFamilyMemberId) : null;
          const b = memberMap.has(r.toFamilyMemberId) ? cardCenter(r.toFamilyMemberId) : null;
          return a && b ? <line key={r.familyRelationshipId} x1={a.x} y1={a.y} x2={b.x} y2={b.y} className={r.relationshipType === 'Spouse' ? 'spouse-line' : 'parent-line'} /> : null;
        })}
      </svg>
      {active.map((m, i) => {
        const p = pointFor(m.familyMemberId, i);
        const selected = connectFrom === m.familyMemberId;
        return <div key={m.familyMemberId} data-family-member-id={m.familyMemberId} className={`editor-person ${selected ? 'connection-source' : ''} ${drag?.id === m.familyMemberId ? 'moving' : ''}`} style={{ left: p.x, top: p.y }} onPointerDown={e => startMove(e, m.familyMemberId)} onClick={() => { if (connectFrom) chooseConnectionTarget(m.familyMemberId); else setProfile(m); }}>
          <div className="editor-person-main"><span className="tree-avatar"><Users size={18}/></span><strong>{m.fullName}</strong>{m.tamilName && <small>{m.tamilName}</small>}<em>{m.status}</em></div>
          {canEdit && <button className="connect-handle" title={connectFrom === m.familyMemberId ? 'Selected as first person' : 'Select this person to connect'} onPointerDown={e => e.stopPropagation()} onClick={e => { e.stopPropagation(); chooseConnectionTarget(m.familyMemberId); }}><Link2 size={14}/>{selected ? 'Selected' : 'Connect'}</button>}
        </div>;
      })}
      {active.length === 0 && <div className="tree-empty"><Users size={28}/><strong>No family members yet</strong><span>Add member profiles before building the family tree.</span></div>}
    </div>
    {relationships.length > 0 && <div className="editor-relationship-list"><h3>Saved relationships</h3>{relationships.map(r => <div className="editor-relationship-row" key={r.familyRelationshipId}><span>{r.fromFullName}</span><b>{r.relationshipType === 'ParentChild' ? 'Parent of' : 'Spouse of'}</b><span>{r.toFullName}</span>{canEdit && <button disabled={busy} onClick={() => remove(r.familyRelationshipId)} title="Remove relationship"><Trash2 size={14}/></button>}</div>)}</div>}
    {profile && <div className="modal-backdrop" onClick={() => setProfile(null)}><div className="modal profile-modal" onClick={e => e.stopPropagation()}><button className="modal-close" onClick={() => setProfile(null)}><X size={16}/></button><span className="eyebrow">FAMILY MEMBER PROFILE</span><div className="profile-heading"><span className="tree-avatar"><Users size={22}/></span><div><h2>{profile.fullName}</h2>{profile.tamilName && <p>{profile.tamilName}</p>}</div></div><div className="profile-facts">{profile.gender && <div><small>Gender</small><strong>{profile.gender}</strong></div>}{profile.dateOfBirth && <div><small>Date of birth</small><strong>{new Date(profile.dateOfBirth + 'T00:00:00').toLocaleDateString()}</strong></div>}{profile.dateOfDeath && <div><small>Date of death</small><strong>{new Date(profile.dateOfDeath + 'T00:00:00').toLocaleDateString()}</strong></div>}{profile.placeOfBirth && <div><small>Place of birth</small><strong>{profile.placeOfBirth}</strong></div>}{profile.contactNumber && <div><small>Contact</small><strong>{profile.contactNumber}</strong></div>}{profile.email && <div><small>Email</small><strong>{profile.email}</strong></div>}<div><small>Status</small><strong>{profile.status}</strong></div></div><div className="profile-relatives"><h3>Family relationships</h3>{relatives.length === 0 ? <p className="muted">No linked relationships recorded for this person.</p> : relatives.map((r, i) => <div className="profile-relative" key={r.label + r.member.familyMemberId + i}><span>{r.label}</span><strong>{r.member.fullName}</strong>{r.member.tamilName && <small>{r.member.tamilName}</small>}</div>)}</div><div className="modal-actions"><button className="secondary-btn" onClick={() => setProfile(null)}>Close</button></div></div></div>}
    {showForm && <div className="modal-backdrop" onClick={() => setShowForm(false)}><div className="modal" onClick={e => e.stopPropagation()}><button className="modal-close" onClick={() => setShowForm(false)}><X size={16}/></button><span className="eyebrow">FAMILY RELATIONSHIP</span><h2>Confirm relationship</h2><label>Relationship type<select value={kind} onChange={e => setKind(e.target.value as 'ParentChild' | 'Spouse')}><option value="ParentChild">First person is Parent → second person is Child</option><option value="Spouse">Spouse (either direction)</option></select></label><label>First person<select value={connectFrom} onChange={e => setConnectFrom(e.target.value)}><option value="">Select member</option>{active.map(m => <option key={m.familyMemberId} value={m.familyMemberId}>{m.fullName}</option>)}</select></label><label>Second person<select value={targetId} onChange={e => setTargetId(e.target.value)}><option value="">Select member</option>{active.filter(m => m.familyMemberId !== connectFrom).map(m => <option key={m.familyMemberId} value={m.familyMemberId}>{m.fullName}</option>)}</select></label><div className="modal-actions"><button className="secondary-btn" onClick={() => { setShowForm(false); setConnectFrom(''); setTargetId(''); }}>Cancel</button><button className="primary-btn" disabled={busy || !connectFrom || !targetId} onClick={save}>Save relationship</button></div></div></div>}
  </section>;
}
