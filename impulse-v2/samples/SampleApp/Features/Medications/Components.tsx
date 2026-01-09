import React from 'react';
import { Link } from '@tanstack/react-router';
import type {
  ListMedicationsResponse,
  GetMedicationResponse,
  MedicationSummary,
  MedicationStatus,
  AdministrationRecord,
  Dosage,
} from '../../generated/types';

// ========================================
// Medications List Component
// ========================================

interface MedicationsListProps {
  data: ListMedicationsResponse;
}

export function MedicationsList({ data }: MedicationsListProps) {
  return (
    <div className="medications-list">
      <header className="page-header">
        <div>
          <Link
            to="/residents/$id"
            params={{ id: String(data.residentId) }}
            className="back-link"
          >
            &larr; Back to Resident
          </Link>
          <h1>Medications</h1>
        </div>
        <span className="badge">{data.medications?.length || 0} active</span>
      </header>

      <div className="table-container">
        <table className="data-table">
          <thead>
            <tr>
              <th>Drug Name</th>
              <th>Dosage</th>
              <th>Route</th>
              <th>Schedule</th>
              <th>Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {data.medications?.map((med) => (
              <MedicationRow
                key={med.id}
                medication={med}
                residentId={data.residentId}
              />
            ))}
          </tbody>
        </table>
      </div>

      {(!data.medications || data.medications.length === 0) && (
        <p className="empty-state">No medications on record.</p>
      )}
    </div>
  );
}

function MedicationRow({
  medication,
  residentId,
}: {
  medication: MedicationSummary;
  residentId: number;
}) {
  return (
    <tr>
      <td className="drug-name">{medication.drugName}</td>
      <td>{medication.dosageDescription}</td>
      <td>{medication.routeDescription}</td>
      <td>{medication.scheduleDescription}</td>
      <td>
        <MedicationStatusBadge status={medication.status} />
      </td>
      <td>
        <Link
          to="/residents/$residentId/medications/$medicationId"
          params={{
            residentId: String(residentId),
            medicationId: String(medication.id),
          }}
          className="btn btn-small"
        >
          View Details
        </Link>
      </td>
    </tr>
  );
}

// ========================================
// Medication Detail Component
// ========================================

interface MedicationDetailProps {
  data: GetMedicationResponse;
}

export function MedicationDetail({ data }: MedicationDetailProps) {
  return (
    <div className="medication-detail">
      <header className="page-header">
        <div>
          <Link
            to="/residents/$residentId/medications"
            params={{ residentId: String(data.residentId) }}
            className="back-link"
          >
            &larr; Back to Medications
          </Link>
          <h1>{data.drugName}</h1>
          {data.genericName && data.genericName !== data.drugName && (
            <p className="subtitle">Generic: {data.genericName}</p>
          )}
        </div>
        <MedicationStatusBadge status={data.status} />
      </header>

      <div className="detail-grid">
        {/* Dosage Information */}
        <section className="card">
          <h2>Dosage Information</h2>
          <dl className="info-list">
            <dt>Prescribed Dosage</dt>
            <dd>
              <DosageDisplay dosage={data.prescribedDosage} />
            </dd>

            <dt>Route</dt>
            <dd>{data.route}</dd>

            <dt>Schedule</dt>
            <dd>
              {data.schedule?.frequencyDescription}
              {data.schedule?.times && data.schedule.times.length > 0 && (
                <span className="schedule-times">
                  {' '}({data.schedule.times.join(', ')})
                </span>
              )}
            </dd>

            <dt>Start Date</dt>
            <dd>{data.schedule?.startDate ? formatDate(data.schedule.startDate) : 'N/A'}</dd>

            {data.schedule?.endDate && (
              <>
                <dt>End Date</dt>
                <dd>{formatDate(data.schedule.endDate)}</dd>
              </>
            )}
          </dl>
        </section>

        {/* Prescriber & Pharmacy */}
        <section className="card">
          <h2>Prescriber & Pharmacy</h2>
          <dl className="info-list">
            <dt>Prescriber</dt>
            <dd>{data.prescriber}</dd>

            {data.pharmacy && (
              <>
                <dt>Pharmacy</dt>
                <dd>{data.pharmacy}</dd>
              </>
            )}

            {data.purpose && (
              <>
                <dt>Purpose</dt>
                <dd>{data.purpose}</dd>
              </>
            )}
          </dl>
        </section>

        {/* Warnings */}
        {data.warnings && data.warnings.length > 0 && (
          <section className="card card-warning">
            <h2>Warnings & Precautions</h2>
            <ul className="warning-list">
              {data.warnings.map((warning, i) => (
                <li key={i}>{warning}</li>
              ))}
            </ul>
          </section>
        )}

        {/* Recent Administrations */}
        <section className="card full-width">
          <h2>Recent Administrations</h2>
          {data.recentAdministrations && data.recentAdministrations.length > 0 ? (
            <table className="data-table">
              <thead>
                <tr>
                  <th>Date/Time</th>
                  <th>Administered By</th>
                  <th>Dosage Given</th>
                  <th>Notes</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {data.recentAdministrations.map((admin) => (
                  <AdministrationRow key={admin.id} record={admin} />
                ))}
              </tbody>
            </table>
          ) : (
            <p className="empty-state">No administration records yet.</p>
          )}
        </section>
      </div>
    </div>
  );
}

function AdministrationRow({ record }: { record: AdministrationRecord }) {
  return (
    <tr className={record.wasRefused ? 'row-refused' : ''}>
      <td>{formatDateTime(record.administeredAt)}</td>
      <td>{record.administeredBy}</td>
      <td>
        {record.wasRefused ? (
          <span className="refused">Refused</span>
        ) : (
          <DosageDisplay dosage={record.dosageGiven} />
        )}
      </td>
      <td>{record.notes || record.refusalReason || '-'}</td>
      <td>
        {record.wasRefused ? (
          <span className="badge badge-refused">Refused</span>
        ) : (
          <span className="badge badge-given">Given</span>
        )}
      </td>
    </tr>
  );
}

// ========================================
// Shared Sub-components
// ========================================

function MedicationStatusBadge({ status }: { status: MedicationStatus }) {
  const statusColors: Record<MedicationStatus, string> = {
    Active: 'active',
    Discontinued: 'discontinued',
    OnHold: 'onhold',
    Completed: 'completed',
  };
  return (
    <span className={`badge badge-${statusColors[status]}`}>
      {status}
    </span>
  );
}

function DosageDisplay({ dosage }: { dosage?: Dosage }) {
  if (!dosage) return <span>-</span>;
  return (
    <span className="dosage">
      {dosage.amount} {dosage.unit}
      {dosage.specialInstructions && (
        <em className="instructions"> - {dosage.specialInstructions}</em>
      )}
    </span>
  );
}

// ========================================
// Utilities
// ========================================

function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

function formatDateTime(dateString: string): string {
  return new Date(dateString).toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });
}
