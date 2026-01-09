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
  ProgressBar,
  TagGroup,
  Tag,
  Accordion,
  Disclosure,
  DisclosureTitle,
  DisclosurePanel,
} from '@react-spectrum/s2';
import type {
  GetCarePlanResponse,
  Assessment,
  CareGoal,
  Intervention,
  CareGoalStatus,
  AssessmentType,
  InterventionFrequency,
} from '../../generated/types';
import { getResidentPath } from '../../generated/routes';

// ========================================
// Care Plan Detail Component
// ========================================

interface CarePlanDetailProps {
  data: GetCarePlanResponse;
}

export function CarePlanDetail({ data }: CarePlanDetailProps) {
  const activeGoals = data.goals?.filter(g => g.status === 'Active').length || 0;
  const completedGoals = data.goals?.filter(g => g.status === 'Completed').length || 0;
  const totalGoals = data.goals?.length || 0;
  const progressPercentage = totalGoals > 0 ? Math.round((completedGoals / totalGoals) * 100) : 0;

  return (
    <div className="app-main">
      {/* Header */}
      <div className="page-header">
        <div>
          <Link to={getResidentPath(data.residentId)} className="back-link">
            ← Back to Resident
          </Link>
          <Heading level={1} UNSAFE_style={{ margin: 0 }}>Care Plan</Heading>
          <Text UNSAFE_className="impulse-subtitle">{data.residentName}</Text>
          <div className="impulse-flex impulse-gap-2 impulse-mt-2">
            <CarePlanStatusBadge status={data.status} />
            <Badge variant="informative" size="S">
              Effective: {formatDate(data.effectiveDate)}
            </Badge>
            {data.reviewDate && (
              <Badge variant="notice" size="S">
                Review: {formatDate(data.reviewDate)}
              </Badge>
            )}
          </div>
        </div>
        <ButtonGroup>
          <ActionButton>Print</ActionButton>
          <ActionButton>Edit</ActionButton>
          <Button variant="accent">Add Assessment</Button>
        </ButtonGroup>
      </div>

      {/* Progress Overview */}
      <div className="impulse-flex impulse-gap-6 impulse-wrap impulse-mb-8">
        <Card UNSAFE_className="impulse-progress-card">
          <div className="impulse-flex impulse-flex-col impulse-gap-6">
            <Heading level={2} UNSAFE_style={{ margin: 0 }}>Care Plan Progress</Heading>
            <div className="impulse-flex impulse-items-center impulse-gap-8">
              <div className="impulse-progress-ring">
                <svg viewBox="0 0 100 100">
                  <circle cx="50" cy="50" r="40" className="impulse-progress-ring-bg" />
                  <circle
                    cx="50" cy="50" r="40"
                    className="impulse-progress-ring-fill"
                    strokeDasharray={`${progressPercentage * 2.51} 251`}
                  />
                </svg>
                <div className="impulse-progress-ring-center">
                  <Text UNSAFE_className="impulse-stat-lg">{progressPercentage}%</Text>
                  <Text UNSAFE_className="impulse-muted-xs">Complete</Text>
                </div>
              </div>
              <div className="impulse-flex impulse-flex-col impulse-gap-4 impulse-flex-1">
                <div className="impulse-flex impulse-justify-between">
                  <div className="impulse-flex impulse-flex-col impulse-gap-1">
                    <Text UNSAFE_className="impulse-stat-lg impulse-success">{activeGoals}</Text>
                    <Text UNSAFE_className="impulse-muted-sm">Active Goals</Text>
                  </div>
                  <div className="impulse-flex impulse-flex-col impulse-gap-1">
                    <Text UNSAFE_className="impulse-stat-lg impulse-info">{completedGoals}</Text>
                    <Text UNSAFE_className="impulse-muted-sm">Completed</Text>
                  </div>
                  <div className="impulse-flex impulse-flex-col impulse-gap-1">
                    <Text UNSAFE_className="impulse-stat-lg">{data.assessments?.length || 0}</Text>
                    <Text UNSAFE_className="impulse-muted-sm">Assessments</Text>
                  </div>
                </div>
                <ProgressBar
                  label="Overall progress"
                  value={progressPercentage}
                  minValue={0}
                  maxValue={100}
                />
              </div>
            </div>
          </div>
        </Card>

        <Card UNSAFE_className="impulse-stats-card">
          <div className="impulse-flex impulse-flex-col impulse-gap-4">
            <Heading level={3} UNSAFE_style={{ margin: 0 }}>Quick Stats</Heading>
            <Divider />
            <QuickStat label="Total Interventions" value={data.goals?.reduce((sum, g) => sum + (g.interventions?.length || 0), 0) || 0} />
            <QuickStat label="Due This Week" value={2} highlight />
            <QuickStat label="Days Since Last Review" value={14} />
            <QuickStat label="Care Team Size" value={4} />
          </div>
        </Card>
      </div>

      {/* Assessments Section */}
      <Card UNSAFE_className="impulse-card impulse-mb-6">
        <div className="impulse-flex impulse-justify-between impulse-items-center impulse-mb-4">
          <div className="impulse-flex impulse-items-center impulse-gap-2">
            <Heading level={2} UNSAFE_style={{ margin: 0 }}>Assessments</Heading>
            <Badge variant="neutral" size="S">{data.assessments?.length || 0}</Badge>
          </div>
          <Button variant="secondary">+ New Assessment</Button>
        </div>
        <Divider />

        {data.assessments && data.assessments.length > 0 ? (
          <Accordion UNSAFE_style={{ marginTop: '16px' }}>
            {data.assessments.map((assessment) => (
              <Disclosure key={assessment.id} id={String(assessment.id)}>
                <DisclosureTitle>
                  <div className="impulse-flex impulse-items-center impulse-gap-2 impulse-flex-1">
                    <AssessmentTypeBadge type={assessment.type} />
                    <Text UNSAFE_className="impulse-value">{formatDate(assessment.assessmentDate)}</Text>
                    <Text UNSAFE_className="impulse-muted">by {assessment.assessorName}</Text>
                  </div>
                </DisclosureTitle>
                <DisclosurePanel>
                  <AssessmentContent assessment={assessment} />
                </DisclosurePanel>
              </Disclosure>
            ))}
          </Accordion>
        ) : (
          <div className="empty-state">
            <Heading level={3}>No Assessments</Heading>
            <Text>No assessments have been recorded for this care plan.</Text>
          </div>
        )}
      </Card>

      {/* Care Goals Section */}
      <Card UNSAFE_className="impulse-card impulse-mb-6">
        <div className="impulse-flex impulse-justify-between impulse-items-center impulse-mb-4">
          <div className="impulse-flex impulse-items-center impulse-gap-2">
            <Heading level={2} UNSAFE_style={{ margin: 0 }}>Care Goals</Heading>
            <Badge variant="neutral" size="S">{totalGoals}</Badge>
          </div>
          <div className="impulse-flex impulse-items-center impulse-gap-2">
            <TagGroup aria-label="Filter goals" selectionMode="single">
              <Tag id="all">All</Tag>
              <Tag id="active">Active</Tag>
              <Tag id="completed">Completed</Tag>
            </TagGroup>
            <Button variant="secondary">+ Add Goal</Button>
          </div>
        </div>
        <Divider />

        {data.goals && data.goals.length > 0 ? (
          <div className="impulse-flex impulse-flex-col impulse-gap-6 impulse-mt-4">
            {data.goals.map((goal) => (
              <CareGoalCard key={goal.id} goal={goal} />
            ))}
          </div>
        ) : (
          <div className="empty-state">
            <Heading level={3}>No Goals</Heading>
            <Text>No care goals have been defined for this plan.</Text>
          </div>
        )}
      </Card>

      {/* Notes Section */}
      {data.notes && (
        <Card UNSAFE_className="impulse-card">
          <Heading level={2} UNSAFE_style={{ margin: 0, marginBottom: '16px' }}>Care Plan Notes</Heading>
          <Divider />
          <Text UNSAFE_className="impulse-notes">{data.notes}</Text>
        </Card>
      )}
    </div>
  );
}

// ========================================
// Quick Stat Component
// ========================================

function QuickStat({ label, value, highlight }: { label: string; value: number; highlight?: boolean }) {
  return (
    <div className="impulse-flex impulse-justify-between impulse-items-center">
      <Text UNSAFE_className="impulse-muted">{label}</Text>
      <Text UNSAFE_className={highlight ? 'impulse-stat-highlight' : 'impulse-stat-sm'}>{value}</Text>
    </div>
  );
}

// ========================================
// Assessment Content
// ========================================

function AssessmentContent({ assessment }: { assessment: Assessment }) {
  return (
    <div className="impulse-flex impulse-flex-col impulse-gap-6 impulse-p-4">
      {/* Findings */}
      <div className="impulse-flex impulse-flex-col impulse-gap-4">
        <Heading level={4} UNSAFE_style={{ margin: 0 }}>Findings</Heading>
        <div className="impulse-findings-grid">
          {assessment.findings && Object.entries(assessment.findings).map(([key, value]) => (
            <Card key={key} UNSAFE_className="impulse-finding-card">
              <div className="impulse-flex impulse-flex-col impulse-gap-1">
                <Text UNSAFE_className="impulse-label-upper-sm">{key}</Text>
                <Text UNSAFE_className="impulse-value">{value}</Text>
              </div>
            </Card>
          ))}
        </div>
      </div>

      {/* Recommendations */}
      {assessment.recommendations && assessment.recommendations.length > 0 && (
        <div className="impulse-flex impulse-flex-col impulse-gap-4">
          <Heading level={4} UNSAFE_style={{ margin: 0 }}>Recommendations</Heading>
          <div className="impulse-flex impulse-flex-col impulse-gap-1">
            {assessment.recommendations.map((rec, i) => (
              <div key={i} className="impulse-flex impulse-items-center impulse-gap-2">
                <Text UNSAFE_className="impulse-check">✓</Text>
                <Text>{rec}</Text>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

// ========================================
// Care Goal Card
// ========================================

function CareGoalCard({ goal }: { goal: CareGoal }) {
  const getStatusVariant = (status: CareGoalStatus): 'positive' | 'negative' | 'notice' | 'informative' | 'neutral' => {
    switch (status) {
      case 'Active': return 'positive';
      case 'Completed': return 'informative';
      case 'OnHold': return 'notice';
      case 'Cancelled': return 'negative';
      default: return 'neutral';
    }
  };

  const daysRemaining = Math.ceil((new Date(goal.targetDate).getTime() - Date.now()) / (1000 * 60 * 60 * 24));
  const isOverdue = daysRemaining < 0;

  const borderColor = goal.status === 'Active' ? '#10b981' :
                      goal.status === 'Completed' ? '#3b82f6' :
                      goal.status === 'OnHold' ? '#f59e0b' : '#9ca3af';

  return (
    <Card UNSAFE_style={{ borderLeft: `4px solid ${borderColor}`, padding: '20px' }}>
      <div className="impulse-flex impulse-flex-col impulse-gap-4">
        {/* Goal Header */}
        <div className="impulse-flex impulse-justify-between impulse-items-start">
          <div className="impulse-flex impulse-flex-col impulse-gap-1 impulse-flex-1">
            <div className="impulse-flex impulse-items-center impulse-gap-2">
              <Badge variant="neutral" size="S">{goal.category}</Badge>
              <StatusLight variant={getStatusVariant(goal.status)}>
                {goal.status}
              </StatusLight>
            </div>
            <Heading level={3} UNSAFE_style={{ margin: 0 }}>{goal.description}</Heading>
          </div>
          <div className="impulse-flex impulse-flex-col impulse-gap-1 impulse-items-end">
            <Text UNSAFE_className="impulse-muted-sm">Target Date</Text>
            <Text UNSAFE_className={isOverdue ? 'impulse-overdue' : 'impulse-value'}>
              {formatDate(goal.targetDate)}
            </Text>
            {goal.status === 'Active' && (
              <Badge variant={isOverdue ? 'negative' : daysRemaining <= 7 ? 'notice' : 'positive'} size="S">
                {isOverdue ? `${Math.abs(daysRemaining)} days overdue` : `${daysRemaining} days left`}
              </Badge>
            )}
          </div>
        </div>

        {/* Target Outcome */}
        <div className="impulse-info-card">
          <Text className="impulse-info-label">Target Outcome</Text>
          <Text className="impulse-info-value">{goal.targetOutcome}</Text>
        </div>

        {/* Interventions */}
        {goal.interventions && goal.interventions.length > 0 && (
          <div className="impulse-flex impulse-flex-col impulse-gap-4">
            <div className="impulse-flex impulse-items-center impulse-gap-2">
              <Heading level={4} UNSAFE_style={{ margin: 0 }}>Interventions</Heading>
              <Badge variant="neutral" size="S">{goal.interventions.length}</Badge>
            </div>
            <div className="table-container">
              <table className="data-table">
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
          </div>
        )}
      </div>
    </Card>
  );
}

function InterventionRow({ intervention }: { intervention: Intervention }) {
  return (
    <tr>
      <td>
        <div className="impulse-flex impulse-items-center impulse-gap-2">
          <Text>{intervention.description}</Text>
          {intervention.requiresDocumentation && (
            <Badge variant="notice" size="S">Doc Required</Badge>
          )}
        </div>
      </td>
      <td><FrequencyBadge frequency={intervention.frequency} /></td>
      <td><Text>{intervention.responsibleRole || '-'}</Text></td>
      <td>
        <Text UNSAFE_className={intervention.specialInstructions ? '' : 'impulse-muted'}>
          {intervention.specialInstructions || 'None'}
        </Text>
      </td>
    </tr>
  );
}

// ========================================
// Status Badges
// ========================================

function CarePlanStatusBadge({ status }: { status: string }) {
  const getVariant = (): 'positive' | 'negative' | 'notice' | 'informative' | 'neutral' => {
    switch (status.toLowerCase()) {
      case 'active': return 'positive';
      case 'draft': return 'notice';
      case 'completed': return 'informative';
      case 'archived': return 'neutral';
      default: return 'neutral';
    }
  };

  return <StatusLight variant={getVariant()}>{status}</StatusLight>;
}

function AssessmentTypeBadge({ type }: { type: AssessmentType }) {
  const typeConfig: Record<AssessmentType, { label: string; variant: 'positive' | 'informative' | 'notice' | 'negative' }> = {
    Initial: { label: 'Initial', variant: 'positive' },
    Quarterly: { label: 'Quarterly', variant: 'informative' },
    Annual: { label: 'Annual', variant: 'informative' },
    ChangeInCondition: { label: 'Condition Change', variant: 'notice' },
  };

  const config = typeConfig[type];
  return <Badge variant={config.variant} size="S">{config.label}</Badge>;
}

function FrequencyBadge({ frequency }: { frequency: InterventionFrequency }) {
  const frequencyLabels: Record<InterventionFrequency, string> = {
    AsNeeded: 'PRN',
    Daily: 'Daily',
    BID: 'BID',
    TID: 'TID',
    QID: 'QID',
    Weekly: 'Weekly',
  };

  const getVariant = (): 'positive' | 'informative' | 'notice' | 'neutral' => {
    switch (frequency) {
      case 'Daily': return 'positive';
      case 'BID':
      case 'TID':
      case 'QID': return 'informative';
      case 'AsNeeded': return 'notice';
      default: return 'neutral';
    }
  };

  return <Badge variant={getVariant()} size="S">{frequencyLabels[frequency]}</Badge>;
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
