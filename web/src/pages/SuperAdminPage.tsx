import { useEffect, useMemo, useState } from 'react'
import { clubsApi } from '../api/services'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { ClubRole, clubRoleLabel, type ClubMember, type CsvImportResult, type ValidateMember } from '../types'
import { downloadMembersCsvTemplate } from '../utils/membersCsv'
import { AdminActivitiesPanel } from './AdminActivitiesPanel'

type Tab = 'people' | 'activities'

function roleBadgeClass(role: ClubRole) {
  if (role === ClubRole.SuperAdmin) return 'badge-training'
  if (role === ClubRole.Admin) return 'badge-race'
  return 'badge-clubrun'
}

export function SuperAdminPage() {
  const { clubId, clubs, isSuperAdmin } = useAuth()
  const [tab, setTab] = useState<Tab>(isSuperAdmin ? 'people' : 'activities')

  if (!clubId) return <p className="page-loading">Loadingâ€¦</p>

  return (
    <div>
      <h1 className="page-title">Admin</h1>
      <p className="page-subtitle">{clubs.find((c) => c.id === clubId)?.name ?? 'Club'}</p>

      <div className="calendar-view-toggle" role="tablist" aria-label="Admin sections">
        {isSuperAdmin && (
          <button
            type="button"
            role="tab"
            aria-selected={tab === 'people'}
            className={`calendar-view-btn${tab === 'people' ? ' active' : ''}`}
            onClick={() => setTab('people')}
          >
            People
          </button>
        )}
        <button
          type="button"
          role="tab"
          aria-selected={tab === 'activities'}
          className={`calendar-view-btn${tab === 'activities' ? ' active' : ''}`}
          onClick={() => setTab('activities')}
        >
          Activities
        </button>
      </div>

      {tab === 'people' && isSuperAdmin ? <PeoplePanel clubId={clubId} /> : <AdminActivitiesPanel clubId={clubId} />}
    </div>
  )
}

function matchesPersonSearch(query: string, fields: Array<string | undefined>) {
  const q = query.trim().toLowerCase()
  if (!q) return true
  const haystack = fields.filter(Boolean).join(' ').toLowerCase()
  const compact = haystack.replace(/[^a-z0-9]+/g, '')
  const qCompact = q.replace(/[^a-z0-9]+/g, '')
  return haystack.includes(q) || (qCompact.length > 0 && compact.includes(qCompact))
}

const PEOPLE_PAGE_SIZE = 10

function PaginationBar({
  page,
  pageCount,
  total,
  onPage,
}: {
  page: number
  pageCount: number
  total: number
  onPage: (page: number) => void
}) {
  if (total === 0 || pageCount <= 1) return null
  const from = (page - 1) * PEOPLE_PAGE_SIZE + 1
  const to = Math.min(page * PEOPLE_PAGE_SIZE, total)
  return (
    <div className="pager">
      <p className="activity-meta">
        {from}â€“{to} of {total}
      </p>
      <div className="pager-actions">
        <button
          type="button"
          className="btn btn-outline btn-sm"
          disabled={page <= 1}
          onClick={() => onPage(page - 1)}
        >
          Previous
        </button>
        <span className="pager-status">
          Page {page} of {pageCount}
        </span>
        <button
          type="button"
          className="btn btn-outline btn-sm"
          disabled={page >= pageCount}
          onClick={() => onPage(page + 1)}
        >
          Next
        </button>
      </div>
    </div>
  )
}

function PersonStatusToggles({
  isActive,
  registered,
  onActiveChange,
}: {
  isActive: boolean
  registered: boolean
  onActiveChange: (next: boolean) => void
}) {
  return (
    <div className="member-status-toggles">
      <label className="member-active-toggle">
        <input type="checkbox" checked={isActive} onChange={(e) => onActiveChange(e.target.checked)} />
        Active
        {!isActive && <span className="badge badge-race">Lapsed</span>}
      </label>
      <label className="member-active-toggle">
        <input type="checkbox" checked={registered} disabled />
        Registered
      </label>
    </div>
  )
}

function PeoplePanel({ clubId }: { clubId: string }) {
  const [members, setMembers] = useState<ClubMember[]>([])
  const [validateMembers, setValidateMembers] = useState<ValidateMember[]>([])
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [eaNumber, setEaNumber] = useState('')
  const [role, setRole] = useState<ClubRole>(ClubRole.Member)
  const [saving, setSaving] = useState(false)
  const [importing, setImporting] = useState(false)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const [importResult, setImportResult] = useState<CsvImportResult | null>(null)
  const [search, setSearch] = useState('')
  const [memberPage, setMemberPage] = useState(1)
  const [pendingPage, setPendingPage] = useState(1)

  const load = async () => {
    const [memberList, pendingList] = await Promise.all([
      clubsApi.members(clubId),
      clubsApi.validateMembers(clubId),
    ])
    setMembers(memberList)
    setValidateMembers(pendingList)
  }

  useEffect(() => {
    load()
  }, [clubId])

  const addPerson = async (e: React.FormEvent) => {
    e.preventDefault()
    setSaving(true)
    setError('')
    setMessage('')
    try {
      const result = await clubsApi.addValidateMember(clubId, {
        firstName,
        lastName,
        englandAthleticsNumber: eaNumber,
        role,
      })
      setMessage(`Added ${result.firstName} ${result.lastName} to ValidateMembers as ${clubRoleLabel(result.role as ClubRole)}. They become a club member after registering with this last name and England Athletics number.`)
      setFirstName('')
      setLastName('')
      setEaNumber('')
      setRole(ClubRole.Member)
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not add person')
    } finally {
      setSaving(false)
    }
  }

  const downloadTemplate = () => {
    downloadMembersCsvTemplate()
  }

  const uploadCsv = async (file: File) => {
    setImporting(true)
    setError('')
    setMessage('')
    try {
      const preview = await clubsApi.importMembers(clubId, file, true)
      const confirmApply = confirm(
        `Preview: ${preview.added} to add, ${preview.updated} to update, ${preview.skipped} skipped. Import now?`,
      )
      if (!confirmApply) {
        setImportResult(preview)
        return
      }
      const result = await clubsApi.importMembers(clubId, file, false)
      setImportResult(result)
      setMessage(`Imported ${result.added} added, ${result.updated} updated, ${result.skipped} skipped.`)
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not import CSV')
    } finally {
      setImporting(false)
    }
  }

  const changeRole = async (member: ClubMember, next: ClubRole) => {
    if (member.role === next) return
    await clubsApi.updateMember(clubId, member.id, next)
    await load()
  }

  const toggleMember = async (member: ClubMember, isActive: boolean) => {
    setError('')
    setMembers((current) => current.map((row) => (row.id === member.id ? { ...row, isActive } : row)))
    try {
      await clubsApi.setMemberActive(clubId, member.id, isActive)
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not update membership')
      await load()
    }
  }

  const togglePending = async (row: ValidateMember, isActive: boolean) => {
    setError('')
    setValidateMembers((current) => current.map((item) => (item.id === row.id ? { ...item, isActive } : item)))
    try {
      await clubsApi.setValidateMemberActive(clubId, row.id, isActive)
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not update membership')
      await load()
    }
  }

  const pendingRows = useMemo(
    () =>
      validateMembers.filter(
        (row) =>
          !row.claimedUserId &&
          matchesPersonSearch(search, [
            row.firstName,
            row.lastName,
            `${row.firstName} ${row.lastName}`,
            row.englandAthleticsNumber,
            clubRoleLabel(row.role),
          ]),
      ),
    [validateMembers, search],
  )

  const visibleMembers = useMemo(
    () =>
      members.filter((member) =>
        matchesPersonSearch(search, [
          member.firstName,
          member.lastName,
          `${member.firstName} ${member.lastName}`,
          member.email,
          member.englandAthleticsNumber,
          clubRoleLabel(member.role),
        ]),
      ),
    [members, search],
  )

  const pendingUnfiltered = validateMembers.filter((row) => !row.claimedUserId)
  const searching = search.trim().length > 0

  const pendingPageCount = Math.max(1, Math.ceil(pendingRows.length / PEOPLE_PAGE_SIZE))
  const memberPageCount = Math.max(1, Math.ceil(visibleMembers.length / PEOPLE_PAGE_SIZE))
  const pagedPending = pendingRows.slice((pendingPage - 1) * PEOPLE_PAGE_SIZE, pendingPage * PEOPLE_PAGE_SIZE)
  const pagedMembers = visibleMembers.slice((memberPage - 1) * PEOPLE_PAGE_SIZE, memberPage * PEOPLE_PAGE_SIZE)

  useEffect(() => {
    setMemberPage(1)
    setPendingPage(1)
  }, [search, clubId])

  useEffect(() => {
    if (memberPage > memberPageCount) setMemberPage(memberPageCount)
  }, [memberPage, memberPageCount])

  useEffect(() => {
    if (pendingPage > pendingPageCount) setPendingPage(pendingPageCount)
  }, [pendingPage, pendingPageCount])

  return (
    <div className="admin-panel">
      <div className="card">
        <h2 className="admin-card-title">Upload members by CSV</h2>
        <p className="activity-meta">
          Tick Active to keep a membership. Untick to lapse it â€” the person keeps their login and
          password and can be ticked active again. CSV is optional for larger updates.
        </p>
        <div className="admin-row-actions">
          <button type="button" className="btn btn-outline" onClick={downloadTemplate}>
            Download template.csv
          </button>
        </div>
        <div className="form-group" style={{ marginTop: '1rem' }}>
          <label htmlFor="sa-csv">Choose completed CSV</label>
          <input
            id="sa-csv"
            type="file"
            accept=".csv,text/csv"
            disabled={importing}
            onChange={(e) => {
              const file = e.target.files?.[0]
              if (file) void uploadCsv(file)
              e.target.value = ''
            }}
          />
        </div>
        {importResult && (
          <p className="activity-meta">
            Last import: {importResult.added} added, {importResult.updated} updated, {importResult.skipped} skipped
            {importResult.dryRun ? ' (preview)' : ''}.
          </p>
        )}
      </div>

      <form className="card" onSubmit={addPerson}>
        <h2 className="admin-card-title">Add person</h2>
        <div className="form-group">
          <label htmlFor="sa-first">First name</label>
          <input id="sa-first" value={firstName} onChange={(e) => setFirstName(e.target.value)} required />
        </div>
        <div className="form-group">
          <label htmlFor="sa-last">Last name</label>
          <input id="sa-last" value={lastName} onChange={(e) => setLastName(e.target.value)} required />
        </div>
        <div className="form-group">
          <label htmlFor="sa-ea">England Athletics number</label>
          <input id="sa-ea" value={eaNumber} onChange={(e) => setEaNumber(e.target.value)} required />
        </div>
        <div className="form-group">
          <label htmlFor="sa-role">Role</label>
          <select id="sa-role" value={role} onChange={(e) => setRole(Number(e.target.value) as ClubRole)}>
            <option value={ClubRole.Member}>Member</option>
            <option value={ClubRole.Admin}>Admin</option>
            <option value={ClubRole.SuperAdmin}>SuperAdmin</option>
          </select>
        </div>
        {error && <p className="form-error">{error}</p>}
        {message && <p className="volunteer-message">{message}</p>}
        <button type="submit" className="btn btn-primary" disabled={saving}>
          {saving ? 'Savingâ€¦' : `Add ${clubRoleLabel(role).toLowerCase()}`}
        </button>
      </form>

      <div className="card">
        <h2 className="admin-card-title">Find a person</h2>
        <p className="activity-meta">Search by name, email, or England Athletics number, then edit them below.</p>
        <div className="form-group" style={{ marginBottom: 0 }}>
          <label htmlFor="sa-search">Search</label>
          <input
            id="sa-search"
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="e.g. Chan, 1111111, admin@"
            autoComplete="off"
          />
        </div>
        {searching && (
          <p className="activity-meta" style={{ marginTop: '0.65rem' }}>
            {visibleMembers.length + pendingRows.length} match
            {visibleMembers.length + pendingRows.length === 1 ? '' : 'es'}
          </p>
        )}
      </div>

      {(pendingUnfiltered.length > 0 || searching) && pendingRows.length > 0 && (
        <>
          <h2 className="admin-card-title">Waiting to register</h2>
          {pagedPending.map((row) => (
            <div key={row.id} className={`card${row.isActive ? '' : ' card--lapsed'}`}>
              <div className="volunteer-role-header">
                <strong>
                  {row.firstName} {row.lastName}
                </strong>
                <span className={`badge ${roleBadgeClass(row.role)}`}>{clubRoleLabel(row.role)}</span>
              </div>
              <p className="activity-meta">EA number: {row.englandAthleticsNumber}</p>
              <PersonStatusToggles
                isActive={row.isActive}
                registered={false}
                onActiveChange={(next) => void togglePending(row, next)}
              />
            </div>
          ))}
          <PaginationBar
            page={pendingPage}
            pageCount={pendingPageCount}
            total={pendingRows.length}
            onPage={setPendingPage}
          />
        </>
      )}

      <h2 className="admin-card-title">Registered members</h2>
      {members.length === 0 ? (
        <div className="empty-state card">No members yet.</div>
      ) : visibleMembers.length === 0 ? (
        <div className="empty-state card">No members match that search.</div>
      ) : (
        <>
          {pagedMembers.map((member) => (
          <div key={member.id} className={`card${member.isActive ? '' : ' card--lapsed'}`}>
            <div className="volunteer-role-header">
              <strong>
                {member.firstName} {member.lastName}
              </strong>
              <span className={`badge ${roleBadgeClass(member.role)}`}>{clubRoleLabel(member.role)}</span>
            </div>
            {member.email && <p className="activity-meta">{member.email}</p>}
            {member.englandAthleticsNumber && <p className="activity-meta">EA number: {member.englandAthleticsNumber}</p>}
            <PersonStatusToggles
              isActive={member.isActive}
              registered={true}
              onActiveChange={(next) => void toggleMember(member, next)}
            />
            <div className="admin-row-actions">
              <select
                id={`role-${member.id}`}
                aria-label={`Role for ${member.firstName} ${member.lastName}`}
                className="btn btn-outline btn-sm"
                value={member.role}
                onChange={(e) => void changeRole(member, Number(e.target.value) as ClubRole)}
              >
                <option value={ClubRole.Member}>Member</option>
                <option value={ClubRole.Admin}>Admin</option>
                <option value={ClubRole.SuperAdmin}>SuperAdmin</option>
              </select>
            </div>
          </div>
          ))}
          <PaginationBar
            page={memberPage}
            pageCount={memberPageCount}
            total={visibleMembers.length}
            onPage={setMemberPage}
          />
        </>
      )}
    </div>
  )
}
