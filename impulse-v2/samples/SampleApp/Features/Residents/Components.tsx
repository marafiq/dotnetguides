import React from 'react';
import { Link } from '@tanstack/react-router';
import type {
  ListResidentsResponse,
  GetResidentResponse,
  ResidentSummary,
  Address,
  EmergencyContact,
} from '../../generated/types';

// ========================================
// Residents List Component
// ========================================

interface ResidentsListProps {
  data: ListResidentsResponse;
}

export function ResidentsList({ data }: ResidentsListProps) {
  return (
    <div className="residents-list">
      <header className="page-header">
        <h1>Residents</h1>
        <span className="badge">{data.totalCount} total</span>
      </header>

      <div className="card-grid">
        {data.residents?.map((resident) => (
          <ResidentCard key={resident.id} resident={resident} />
        ))}
      </div>

      {data.residents?.length === 0 && (
        <p className="empty-state">No residents found.</p>
      )}
    </div>
  );
}

function ResidentCard({ resident }: { resident: ResidentSummary }) {
  return (
    <Link
      to="/residents/$id"
      params={{ id: String(resident.id) }}
      className="card resident-card"
    >
      <div className="card-header">
        <h3>{resident.firstName} {resident.lastName}</h3>
        <StatusBadge status={resident.status} />
      </div>
      <div className="card-body">
        <div className="info-row">
          <span className="label">Room</span>
          <span className="value">{resident.roomNumber || 'Unassigned'}</span>
        </div>
        <div className="info-row">
          <span className="label">Age</span>
          <span className="value">{resident.age} years</span>
        </div>
      </div>
    </Link>
  );
}

// ========================================
// Resident Detail Component
// ========================================

interface ResidentDetailProps {
  data: GetResidentResponse;
}

export function ResidentDetail({ data }: ResidentDetailProps) {
  const age = calculateAge(data.dateOfBirth);

  return (
    <div className="resident-detail">
      <header className="page-header">
        <div>
          <Link to="/residents" className="back-link">
            &larr; Back to Residents
          </Link>
          <h1>{data.firstName} {data.lastName}</h1>
        </div>
        <StatusBadge status={data.status} />
      </header>

      <div className="detail-grid">
        {/* Basic Information */}
        <section className="card">
          <h2>Basic Information</h2>
          <dl className="info-list">
            <dt>Date of Birth</dt>
            <dd>{formatDate(data.dateOfBirth)} ({age} years old)</dd>

            <dt>Room Number</dt>
            <dd>{data.roomNumber || 'Unassigned'}</dd>

            <dt>Admission Date</dt>
            <dd>{formatDate(data.admissionDate)}</dd>

            <dt>Status</dt>
            <dd><StatusBadge status={data.status} /></dd>
          </dl>
        </section>

        {/* Address */}
        {data.address && (
          <section className="card">
            <h2>Address</h2>
            <AddressDisplay address={data.address} />
          </section>
        )}

        {/* Emergency Contacts */}
        <section className="card">
          <h2>Emergency Contacts</h2>
          {data.emergencyContacts && data.emergencyContacts.length > 0 ? (
            <div className="contact-list">
              {data.emergencyContacts.map((contact, i) => (
                <ContactCard key={i} contact={contact} />
              ))}
            </div>
          ) : (
            <p className="empty-state">No emergency contacts on file.</p>
          )}
        </section>

        {/* Preferences */}
        {data.preferences && (
          <section className="card">
            <h2>Care Preferences</h2>
            <dl className="info-list">
              {data.preferences.dietaryRestrictions && (
                <>
                  <dt>Dietary Restrictions</dt>
                  <dd>{data.preferences.dietaryRestrictions}</dd>
                </>
              )}
              {data.preferences.mobilityAids && (
                <>
                  <dt>Mobility Aids</dt>
                  <dd>{data.preferences.mobilityAids}</dd>
                </>
              )}
              {data.preferences.communicationPreferences && (
                <>
                  <dt>Communication</dt>
                  <dd>{data.preferences.communicationPreferences}</dd>
                </>
              )}
              <dt>Preferred Care Time</dt>
              <dd>
                {data.preferences.prefersMorningCare && 'Morning'}
                {data.preferences.prefersMorningCare && data.preferences.prefersEveningCare && ' & '}
                {data.preferences.prefersEveningCare && 'Evening'}
                {!data.preferences.prefersMorningCare && !data.preferences.prefersEveningCare && 'No preference'}
              </dd>
            </dl>
          </section>
        )}

        {/* Quick Links */}
        <section className="card">
          <h2>Quick Links</h2>
          <nav className="quick-links">
            <Link
              to="/residents/$residentId/medications"
              params={{ residentId: String(data.id) }}
              className="btn btn-secondary"
            >
              View Medications
            </Link>
            <Link
              to="/residents/$residentId/care-plan"
              params={{ residentId: String(data.id) }}
              className="btn btn-secondary"
            >
              View Care Plan
            </Link>
          </nav>
        </section>
      </div>
    </div>
  );
}

// ========================================
// Shared Sub-components
// ========================================

function StatusBadge({ status }: { status: string }) {
  const className = `badge badge-${status.toLowerCase()}`;
  return <span className={className}>{status}</span>;
}

function AddressDisplay({ address }: { address: Address }) {
  return (
    <address className="address">
      {address.street}<br />
      {address.city}, {address.state} {address.zipCode}
    </address>
  );
}

function ContactCard({ contact }: { contact: EmergencyContact }) {
  return (
    <div className="contact-card">
      <strong>{contact.name}</strong>
      <span className="relationship">({contact.relationship})</span>
      <div className="contact-info">
        <a href={`tel:${contact.phone}`}>{contact.phone}</a>
        {contact.email && (
          <a href={`mailto:${contact.email}`}>{contact.email}</a>
        )}
      </div>
    </div>
  );
}

// ========================================
// Utilities
// ========================================

function calculateAge(dateOfBirth: string): number {
  const dob = new Date(dateOfBirth);
  const today = new Date();
  let age = today.getFullYear() - dob.getFullYear();
  const monthDiff = today.getMonth() - dob.getMonth();
  if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < dob.getDate())) {
    age--;
  }
  return age;
}

function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });
}
