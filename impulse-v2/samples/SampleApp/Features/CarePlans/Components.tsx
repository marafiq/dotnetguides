import React from 'react';
import { Link } from '@tanstack/react-router';
import type {
  GetCarePlanResponse,
  Assessment,
  CareGoal,
  Intervention,
  CareGoalStatus,
  AssessmentType,
  InterventionFrequency,
} from '../../generated/types';

// ========================================
// Care Plan Detail Component
// ========================================

interface CarePlanDetailProps {
  data: GetCarePlanResponse;
}

export function CarePlanDetail({ data }: CarePlanDetailProps) {
  return (
    <div className="care-plan-detail">
      <header className="page-header">
        <div>
          <Link
            to="/residents/$id"
            params={{ id: String(data.residentId) }}
            className="back-link"
          >
            &larr; Back to Resident
          </Link>
          <h1>Care Plan</h1>
          <p className="subtitle">{data.residentName}</p>
        </div>
        <CarePlanStatusBadge status={data.status} />
      </header>

      <div className="care-plan-meta">
        <div className="meta-item">
          <span className="label">Effective Date</span>
          <span className="value">{formatDate(data.effectiveDate)}</span>
        </div>
        {data.reviewDate && (
          <div className="meta-item">
            <span className="label">Next Review</span>
            <span className="value">{formatDate(data.reviewDate)}</span>
          </div>
        )}
      </div>

      {/* Assessments Section */}
      <section className="care-section">
        <h2>Assessments</h2>
        {data.assessments && data.assessments.length > 0 ? (
          <div className="assessments-list">
            {data.assessments.map((assessment) => (
              <AssessmentCard key={assessment.id} assessment={assessment} />
            ))}
          </div>
        ) : (
          <p className="empty-state">No assessments recorded.</p>
        )}
      </section>

      {/* Care Goals Section */}
      <section className="care-section">
        <h2>Care Goals</h2>
        {data.goals && data.goals.length > 0 ? (
          <div className="goals-list">
            {data.goals.map((goal) => (
              <CareGoalCard key={goal.id} goal={goal} />
            ))}
          </div>
        ) : (
          <p className="empty-state">No care goals defined.</p>
        )}
      </section>

      {/* Notes Section */}
      {data.notes && (
        <section className="care-section">
          <h2>Care Plan Notes</h2>
          <div className="card">
            <p className="notes-text">{data.notes}</p>
          </div>
        </section>
      )}
    </div>
  );
}

// ========================================
// Assessment Card
// ========================================

function AssessmentCard({ assessment }: { assessment: Assessment }) {
  return (
    <div className="card assessment-card">
      <div className="card-header">
        <div>
          <AssessmentTypeBadge type={assessment.type} />
          <span className="assessment-date">{formatDate(assessment.assessmentDate)}</span>
        </div>
        <span className="assessor">by {assessment.assessorName}</span>
      </div>

      <div className="card-body">
        <h4>Findings</h4>
        <dl className="findings-list">
          {assessment.findings && Object.entries(assessment.findings).map(([key, value]) => (
            <div key={key} className="finding-item">
              <dt>{key}</dt>
              <dd>{value}</dd>
            </div>
          ))}
        </dl>

        {assessment.recommendations && assessment.recommendations.length > 0 && (
          <>
            <h4>Recommendations</h4>
            <ul className="recommendations-list">
              {assessment.recommendations.map((rec, i) => (
                <li key={i}>{rec}</li>
              ))}
            </ul>
          </>
        )}
      </div>
    </div>
  );
}

// ========================================
// Care Goal Card
// ========================================

function CareGoalCard({ goal }: { goal: CareGoal }) {
  return (
    <div className="card goal-card">
      <div className="card-header">
        <div>
          <span className="goal-category">{goal.category}</span>
          <h3>{goal.description}</h3>
        </div>
        <CareGoalStatusBadge status={goal.status} />
      </div>

      <div className="card-body">
        <div className="goal-target">
          <strong>Target Outcome:</strong> {goal.targetOutcome}
        </div>
        <div className="goal-date">
          <strong>Target Date:</strong> {formatDate(goal.targetDate)}
        </div>

        {goal.interventions && goal.interventions.length > 0 && (
          <div className="interventions-section">
            <h4>Interventions</h4>
            <table className="interventions-table">
              <thead>
                <tr>
                  <th>Intervention</th>
                  <th>Frequency</th>
                  <th>Responsible</th>
                  <th>Instructions</th>
                </tr>
              </thead>
              <tbody>
                {goal.interventions.map((intervention) => (
                  <InterventionRow key={intervention.id} intervention={intervention} />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}

function InterventionRow({ intervention }: { intervention: Intervention }) {
  return (
    <tr>
      <td>
        {intervention.description}
        {intervention.requiresDocumentation && (
          <span className="doc-required" title="Documentation required">*</span>
        )}
      </td>
      <td>
        <FrequencyBadge frequency={intervention.frequency} />
      </td>
      <td>{intervention.responsibleRole || '-'}</td>
      <td>{intervention.specialInstructions || '-'}</td>
    </tr>
  );
}

// ========================================
// Status Badges
// ========================================

function CarePlanStatusBadge({ status }: { status: string }) {
  const className = `badge badge-plan-${status.toLowerCase().replace(/\s+/g, '-')}`;
  return <span className={className}>{status}</span>;
}

function CareGoalStatusBadge({ status }: { status: CareGoalStatus }) {
  const statusColors: Record<CareGoalStatus, string> = {
    Active: 'active',
    Completed: 'completed',
    OnHold: 'onhold',
    Cancelled: 'cancelled',
  };
  return (
    <span className={`badge badge-goal-${statusColors[status]}`}>
      {status}
    </span>
  );
}

function AssessmentTypeBadge({ type }: { type: AssessmentType }) {
  const typeLabels: Record<AssessmentType, string> = {
    Initial: 'Initial Assessment',
    Quarterly: 'Quarterly Review',
    Annual: 'Annual Review',
    ChangeInCondition: 'Change in Condition',
  };
  return <span className="badge badge-assessment">{typeLabels[type]}</span>;
}

function FrequencyBadge({ frequency }: { frequency: InterventionFrequency }) {
  const frequencyLabels: Record<InterventionFrequency, string> = {
    AsNeeded: 'PRN',
    Daily: 'Daily',
    BID: 'BID (2x/day)',
    TID: 'TID (3x/day)',
    QID: 'QID (4x/day)',
    Weekly: 'Weekly',
  };
  return <span className="frequency-badge">{frequencyLabels[frequency]}</span>;
}

// ========================================
// Utilities
// ========================================

function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });
}
