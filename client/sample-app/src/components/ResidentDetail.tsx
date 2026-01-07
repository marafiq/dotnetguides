import { useParams, Link } from '@tanstack/react-router';
import { useDeferred, useLazy } from '@impulse/react';

export function ResidentDetail() {
  const { id } = useParams({ from: '/residents/$id' });

  // In a real app, these would come from route loader and deferred/lazy hooks
  const props = {
    id: Number(id),
    name: 'Margaret Chen',
    room: 'A-101',
    admitDate: '2024-03-15',
    allergies: ['Penicillin', 'Sulfa'],
  };

  // Example of deferred data (auto-loads after hydration)
  const deferredUrls = { medications: `/residents/${id}/medications` };
  const medicationsState = useDeferred<{ medications: any[] }>('medications', deferredUrls);

  // Example of lazy data (loads on demand)
  const lazyUrls = { documents: `/residents/${id}/documents` };
  const [documentsState, loadDocuments] = useLazy<{ documents: any[] }>('documents', lazyUrls);

  return (
    <div>
      <Link to="/residents" style={{ color: '#3b82f6', textDecoration: 'none', fontSize: '0.875rem', marginBottom: '1rem', display: 'block' }}>
        ← Back to Residents
      </Link>

      <div style={{ backgroundColor: 'white', borderRadius: '0.5rem', padding: '1.5rem', boxShadow: '0 1px 3px rgba(0,0,0,0.1)', marginBottom: '1.5rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
          <div>
            <h1 style={{ fontSize: '1.5rem', fontWeight: 'bold', marginBottom: '0.5rem' }}>{props.name}</h1>
            <div style={{ color: '#6b7280' }}>Room {props.room}</div>
          </div>
          <button style={{
            backgroundColor: '#3b82f6',
            color: 'white',
            padding: '0.5rem 1rem',
            borderRadius: '0.375rem',
            border: 'none',
            cursor: 'pointer'
          }}>
            Edit
          </button>
        </div>

        <div style={{ marginTop: '1.5rem', display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
          <div>
            <div style={{ fontSize: '0.875rem', color: '#6b7280' }}>Admit Date</div>
            <div style={{ fontWeight: '500' }}>{new Date(props.admitDate).toLocaleDateString()}</div>
          </div>
          <div>
            <div style={{ fontSize: '0.875rem', color: '#6b7280' }}>Allergies</div>
            <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
              {props.allergies.length > 0 ? (
                props.allergies.map((allergy) => (
                  <span key={allergy} style={{
                    backgroundColor: '#fef2f2',
                    color: '#dc2626',
                    padding: '0.25rem 0.5rem',
                    borderRadius: '0.25rem',
                    fontSize: '0.875rem'
                  }}>
                    {allergy}
                  </span>
                ))
              ) : (
                <span style={{ color: '#6b7280' }}>None reported</span>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Medications (Deferred - auto-loads) */}
      <div style={{ backgroundColor: 'white', borderRadius: '0.5rem', padding: '1.5rem', boxShadow: '0 1px 3px rgba(0,0,0,0.1)', marginBottom: '1.5rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
          <h2 style={{ fontSize: '1.125rem', fontWeight: '600' }}>Medications</h2>
          <button style={{
            backgroundColor: '#10b981',
            color: 'white',
            padding: '0.5rem 1rem',
            borderRadius: '0.375rem',
            border: 'none',
            cursor: 'pointer',
            fontSize: '0.875rem'
          }}>
            + Add Medication
          </button>
        </div>

        {medicationsState.status === 'loading' && (
          <div style={{ color: '#6b7280', textAlign: 'center', padding: '2rem' }}>Loading medications...</div>
        )}
        {medicationsState.status === 'error' && (
          <div style={{ color: '#dc2626', textAlign: 'center', padding: '2rem' }}>Failed to load medications</div>
        )}
        {medicationsState.status === 'idle' && (
          <div style={{ color: '#6b7280', textAlign: 'center', padding: '2rem' }}>Medications will load automatically...</div>
        )}
        {medicationsState.status === 'success' && (
          <div style={{ color: '#6b7280' }}>
            {medicationsState.data.medications?.length || 0} medications loaded
          </div>
        )}
      </div>

      {/* Documents (Lazy - loads on demand) */}
      <div style={{ backgroundColor: 'white', borderRadius: '0.5rem', padding: '1.5rem', boxShadow: '0 1px 3px rgba(0,0,0,0.1)' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
          <h2 style={{ fontSize: '1.125rem', fontWeight: '600' }}>Documents</h2>
          {documentsState.status === 'idle' && (
            <button
              onClick={loadDocuments}
              style={{
                backgroundColor: '#6366f1',
                color: 'white',
                padding: '0.5rem 1rem',
                borderRadius: '0.375rem',
                border: 'none',
                cursor: 'pointer',
                fontSize: '0.875rem'
              }}
            >
              Load Documents
            </button>
          )}
        </div>

        {documentsState.status === 'idle' && (
          <div style={{ color: '#6b7280', textAlign: 'center', padding: '2rem' }}>
            Click "Load Documents" to view documents (lazy loading demo)
          </div>
        )}
        {documentsState.status === 'loading' && (
          <div style={{ color: '#6b7280', textAlign: 'center', padding: '2rem' }}>Loading documents...</div>
        )}
        {documentsState.status === 'error' && (
          <div style={{ color: '#dc2626', textAlign: 'center', padding: '2rem' }}>Failed to load documents</div>
        )}
        {documentsState.status === 'success' && (
          <div style={{ color: '#6b7280' }}>
            {documentsState.data.documents?.length || 0} documents loaded
          </div>
        )}
      </div>
    </div>
  );
}
