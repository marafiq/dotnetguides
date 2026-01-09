import React from 'react';
import { Link } from '@tanstack/react-router';
import {
  Card,
  Badge,
  Button,
  ButtonGroup,
  Heading,
  Text,
  Divider,
  ActionButton,
  StatusLight,
  Meter,
  TagGroup,
  Tag,
} from '@react-spectrum/s2';
import type {
  ListMedicationsResponse,
  GetMedicationResponse,
  MedicationSummary,
  MedicationStatus,
  AdministrationRecord,
  Dosage,
} from '../../generated/types';
import {
  getResidentPath,
  listMedicationsPath,
  getMedicationPath,
} from '../../generated/routes';

// ========================================
// Medications List Component
// ========================================

interface MedicationsListProps {
  data: ListMedicationsResponse;
}

export function MedicationsList({ data }: MedicationsListProps) {
  const activeCount = data.medications?.filter(m => m.status === 'Active').length || 0;
  const onHoldCount = data.medications?.filter(m => m.status === 'OnHold').length || 0;
  const discontinuedCount = data.medications?.filter(m => m.status === 'Discontinued').length || 0;
  const totalCount = data.medications?.length || 0;

  return (
    <div className="app-main">
      {/* Header */}
      <div className="page-header">
        <div>
          <Link to={getResidentPath(data.residentId)} className="back-link">
            ← Back to Resident
          </Link>
          <div className="impulse-flex impulse-items-center impulse-gap-4">
            <Heading level={1}>Medications</Heading>
            <Badge variant="informative" size="L">{totalCount} Total</Badge>
          </div>
        </div>
        <Button variant="accent">+ Add Medication</Button>
      </div>

      {/* Summary Cards */}
      <div className="impulse-flex impulse-gap-6 impulse-wrap impulse-mb-8">
        <Card UNSAFE_className="impulse-summary-card">
          <div className="impulse-flex impulse-flex-col impulse-gap-4">
            <Text UNSAFE_className="impulse-label-upper">Medication Compliance</Text>
            <Meter
              label="Active medications administered on time"
              value={85}
              variant="positive"
            />
            <Text UNSAFE_className="impulse-muted-sm">
              85% of doses administered within scheduled window
            </Text>
          </div>
        </Card>

        <Card UNSAFE_className="impulse-summary-card-sm">
          <div className="impulse-flex impulse-flex-col impulse-gap-4">
            <Text UNSAFE_className="impulse-label-upper">Next Administration</Text>
            <div className="impulse-flex impulse-items-center impulse-gap-4">
              <Text UNSAFE_className="impulse-stat-value">2:00 PM</Text>
              <Text UNSAFE_className="impulse-muted">in 45 minutes</Text>
            </div>
            <Badge variant="notice" size="S">3 medications due</Badge>
          </div>
        </Card>

        <Card UNSAFE_className="impulse-summary-card-sm">
          <div className="impulse-flex impulse-flex-col impulse-gap-4">
            <Text UNSAFE_className="impulse-label-upper">Medication Status</Text>
            <div className="impulse-flex impulse-gap-4 impulse-justify-around">
              <div className="impulse-flex impulse-flex-col impulse-items-center impulse-gap-1">
                <Text UNSAFE_className="impulse-stat-md">{activeCount}</Text>
                <Text UNSAFE_className="impulse-muted-xs">Active</Text>
              </div>
              <div className="impulse-flex impulse-flex-col impulse-items-center impulse-gap-1">
                <Text UNSAFE_className="impulse-stat-md">{onHoldCount}</Text>
                <Text UNSAFE_className="impulse-muted-xs">On Hold</Text>
              </div>
              <div className="impulse-flex impulse-flex-col impulse-items-center impulse-gap-1">
                <Text UNSAFE_className="impulse-stat-md">{discontinuedCount}</Text>
                <Text UNSAFE_className="impulse-muted-xs">Discontinued</Text>
              </div>
            </div>
          </div>
        </Card>
      </div>

      {/* Medications Table */}
      <Card UNSAFE_className="impulse-card">
        <div className="impulse-flex impulse-justify-between impulse-items-center impulse-mb-5">
          <Heading level={2} UNSAFE_style={{ margin: 0 }}>All Medications</Heading>
          <TagGroup aria-label="Filter medications" selectionMode="single">
            <Tag id="all">All</Tag>
            <Tag id="active">Active</Tag>
            <Tag id="prn">PRN</Tag>
            <Tag id="controlled">Controlled</Tag>
          </TagGroup>
        </div>

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
          <div className="empty-state">
            <Heading level={3}>No Medications</Heading>
            <Text>No medications are currently prescribed for this resident.</Text>
          </div>
        )}
      </Card>
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
  const getStatusVariant = (status: MedicationStatus): 'positive' | 'negative' | 'notice' | 'informative' | 'neutral' => {
    switch (status) {
      case 'Active': return 'positive';
      case 'Discontinued': return 'negative';
      case 'OnHold': return 'notice';
      case 'Completed': return 'neutral';
      default: return 'informative';
    }
  };

  return (
    <tr>
      <td>
        <div className="impulse-flex impulse-flex-col impulse-gap-1">
          <Text UNSAFE_className="impulse-value">{medication.drugName}</Text>
          <Text UNSAFE_className="impulse-muted-sm">Last given: Today 8:00 AM</Text>
        </div>
      </td>
      <td><Text>{medication.dosageDescription}</Text></td>
      <td><Badge variant="neutral" size="S">{medication.routeDescription}</Badge></td>
      <td><Text>{medication.scheduleDescription}</Text></td>
      <td>
        <StatusLight variant={getStatusVariant(medication.status)}>
          {medication.status}
        </StatusLight>
      </td>
      <td>
        <Link to={getMedicationPath(residentId, medication.id)}>
          <ActionButton>View</ActionButton>
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
  const getStatusVariant = (status: MedicationStatus): 'positive' | 'negative' | 'notice' | 'informative' | 'neutral' => {
    switch (status) {
      case 'Active': return 'positive';
      case 'Discontinued': return 'negative';
      case 'OnHold': return 'notice';
      case 'Completed': return 'neutral';
      default: return 'informative';
    }
  };

  return (
    <div className="app-main">
      {/* Header */}
      <div className="page-header">
        <div>
          <Link to={listMedicationsPath(data.residentId)} className="back-link">
            ← Back to Medications
          </Link>
          <Heading level={1} UNSAFE_style={{ margin: 0 }}>{data.drugName}</Heading>
          {data.genericName && data.genericName !== data.drugName && (
            <Text UNSAFE_className="impulse-muted">Generic: {data.genericName}</Text>
          )}
          <div className="impulse-flex impulse-gap-2 impulse-mt-2">
            <StatusLight variant={getStatusVariant(data.status)}>{data.status}</StatusLight>
            {data.purpose && <Badge variant="informative" size="S">{data.purpose}</Badge>}
          </div>
        </div>
        <ButtonGroup>
          <ActionButton>Edit</ActionButton>
          <Button variant="accent">Administer Now</Button>
        </ButtonGroup>
      </div>

      {/* Content Grid */}
      <div className="detail-grid">
        {/* Dosage Information */}
        <Card UNSAFE_className="impulse-card">
          <Heading level={2}>Dosage Information</Heading>
          <Divider />
          <div className="info-list">
            <InfoRow label="Prescribed Dosage" value={formatDosage(data.prescribedDosage)} />
            <InfoRow label="Route" value={data.route} />
            <InfoRow label="Schedule" value={data.schedule?.frequencyDescription || 'N/A'} />
            {data.schedule?.times && data.schedule.times.length > 0 && (
              <div className="info-row">
                <span className="label">Times</span>
                <TagGroup aria-label="Administration times">
                  {data.schedule.times.map((time, i) => (
                    <Tag key={i}>{time}</Tag>
                  ))}
                </TagGroup>
              </div>
            )}
            <InfoRow label="Start Date" value={data.schedule?.startDate ? formatDate(data.schedule.startDate) : 'N/A'} />
            {data.schedule?.endDate && (
              <InfoRow label="End Date" value={formatDate(data.schedule.endDate)} />
            )}
          </div>
        </Card>

        {/* Prescriber & Pharmacy */}
        <Card UNSAFE_className="impulse-card">
          <Heading level={2}>Prescriber & Pharmacy</Heading>
          <Divider />
          <div className="info-list">
            <InfoRow label="Prescriber" value={data.prescriber} />
            {data.pharmacy && <InfoRow label="Pharmacy" value={data.pharmacy} />}
            {data.purpose && <InfoRow label="Purpose" value={data.purpose} />}
          </div>
        </Card>

        {/* Warnings */}
        {data.warnings && data.warnings.length > 0 && (
          <Card UNSAFE_className="impulse-warning-card">
            <div className="impulse-flex impulse-items-center impulse-gap-2 impulse-mb-4">
              <Heading level={2} UNSAFE_className="impulse-warning-title">
                Warnings & Precautions
              </Heading>
            </div>
            <ul className="warning-list">
              {data.warnings.map((warning, i) => (
                <li key={i}><Text>{warning}</Text></li>
              ))}
            </ul>
          </Card>
        )}

        {/* Recent Administrations */}
        <Card UNSAFE_className="impulse-card impulse-full-width">
          <div className="impulse-flex impulse-justify-between impulse-items-center impulse-mb-4">
            <Heading level={2} UNSAFE_style={{ margin: 0 }}>Recent Administrations</Heading>
            <Badge variant="neutral" size="S">{data.recentAdministrations?.length || 0} records</Badge>
          </div>
          <Divider />

          {data.recentAdministrations && data.recentAdministrations.length > 0 ? (
            <div className="table-container">
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
            </div>
          ) : (
            <div className="empty-state">
              <Heading level={3}>No Records</Heading>
              <Text>No administration records yet.</Text>
            </div>
          )}
        </Card>
      </div>
    </div>
  );
}

function AdministrationRow({ record }: { record: AdministrationRecord }) {
  return (
    <tr className={record.wasRefused ? 'row-refused' : ''}>
      <td><Text UNSAFE_className="impulse-value">{formatDateTime(record.administeredAt)}</Text></td>
      <td>
        <div className="impulse-flex impulse-items-center impulse-gap-2">
          <div className="impulse-avatar-sm">
            {record.administeredBy.split(' ').map(n => n[0]).join('')}
          </div>
          <Text>{record.administeredBy}</Text>
        </div>
      </td>
      <td>
        {record.wasRefused ? (
          <Text UNSAFE_className="impulse-refused">Refused</Text>
        ) : (
          <Text>{formatDosage(record.dosageGiven)}</Text>
        )}
      </td>
      <td>
        <Text UNSAFE_className="impulse-muted">
          {record.notes || record.refusalReason || '-'}
        </Text>
      </td>
      <td>
        <Badge variant={record.wasRefused ? 'negative' : 'positive'} size="S">
          {record.wasRefused ? 'Refused' : 'Given'}
        </Badge>
      </td>
    </tr>
  );
}

// ========================================
// Shared Sub-components
// ========================================

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="info-row">
      <span className="label">{label}</span>
      <span className="value">{value}</span>
    </div>
  );
}

function formatDosage(dosage?: Dosage): string {
  if (!dosage) return '-';
  let result = `${dosage.amount} ${dosage.unit}`;
  if (dosage.specialInstructions) {
    result += ` - ${dosage.specialInstructions}`;
  }
  return result;
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
