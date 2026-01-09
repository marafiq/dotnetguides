import React from 'react';
import { Link } from '@tanstack/react-router';
import {
  Card,
  Badge,
  Button,
  Heading,
  Text,
  Divider,
  StatusLight,
  Meter,
  ProgressBar,
  ActionButton,
} from '@react-spectrum/s2';
import type {
  GetDashboardResponse,
  DashboardStats,
  RecentActivity,
  UpcomingTask,
  ResidentQuickView,
  MedicationComplianceData,
  DailyScheduleItem,
} from '../../generated/types';
import { getResidentPath, listMedicationsPath } from '../../generated/routes';
import { formatRelativeTime, formatTime } from '../../client/shared/utils';

// ========================================
// Dashboard Overview Component
// Demonstrates complex server-driven UI
// All data is aggregated server-side - this is the Impulse way
// ========================================

interface DashboardProps {
  data: GetDashboardResponse;
}

export function Dashboard({ data }: DashboardProps) {
  return (
    <div className="app-main">
      {/* Header */}
      <div className="page-header">
        <div>
          <Heading level={1}>Dashboard</Heading>
          <Text UNSAFE_className="impulse-muted">
            Senior Living Facility Overview - {new Date().toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' })}
          </Text>
        </div>
        <div className="impulse-flex impulse-gap-2">
          <ActionButton>Export Report</ActionButton>
          <Button variant="accent">+ Quick Admit</Button>
        </div>
      </div>

      {/* Stats Grid */}
      <StatsGrid stats={data.stats} />

      {/* Main Content Grid */}
      <div className="dashboard-grid">
        {/* Left Column */}
        <div className="impulse-flex impulse-flex-col impulse-gap-6">
          {/* Residents Needing Attention */}
          <ResidentsAttentionCard residents={data.residentsNeedingAttention} />

          {/* Upcoming Tasks */}
          <UpcomingTasksCard tasks={data.upcomingTasks} />
        </div>

        {/* Right Column */}
        <div className="impulse-flex impulse-flex-col impulse-gap-6">
          {/* Medication Compliance */}
          <ComplianceCard compliance={data.medicationCompliance} />

          {/* Today's Schedule */}
          <ScheduleCard schedule={data.todaysSchedule} />

          {/* Recent Activity */}
          <RecentActivityCard activities={data.recentActivities} />
        </div>
      </div>
    </div>
  );
}

// ========================================
// Stats Grid Component
// ========================================

function StatsGrid({ stats }: { stats: DashboardStats }) {
  return (
    <div className="impulse-flex impulse-gap-4 impulse-wrap impulse-mb-6">
      <StatCard
        label="Total Residents"
        value={stats.totalResidents}
        variant="informative"
        detail={`${stats.activeResidents} active`}
      />
      <StatCard
        label="Medications Due"
        value={stats.medicationsDueToday}
        variant={stats.overdueMedications > 0 ? 'negative' : 'positive'}
        detail={stats.overdueMedications > 0 ? `${stats.overdueMedications} overdue` : 'All on track'}
      />
      <StatCard
        label="Active Care Plans"
        value={stats.activeCarePlans}
        variant="positive"
        detail={`${stats.upcomingAssessments} assessments due`}
      />
      <StatCard
        label="Hospitalized"
        value={stats.hospitalizedResidents}
        variant={stats.hospitalizedResidents > 0 ? 'notice' : 'neutral'}
        detail={stats.onLeaveResidents > 0 ? `${stats.onLeaveResidents} on leave` : 'None on leave'}
      />
    </div>
  );
}

function StatCard({ label, value, variant, detail }: {
  label: string;
  value: number;
  variant: 'positive' | 'negative' | 'notice' | 'informative' | 'neutral';
  detail: string;
}) {
  return (
    <Card UNSAFE_className="impulse-stat-card">
      <div className="impulse-flex impulse-flex-col impulse-items-center impulse-gap-2">
        <Text UNSAFE_className="impulse-label-upper">{label}</Text>
        <Text UNSAFE_className="impulse-stat-value">{value}</Text>
        <Badge variant={variant} size="S">{detail}</Badge>
      </div>
    </Card>
  );
}

// ========================================
// Residents Needing Attention
// ========================================

function ResidentsAttentionCard({ residents }: { residents?: readonly ResidentQuickView[] }) {
  return (
    <Card UNSAFE_className="impulse-card">
      <div className="impulse-flex impulse-justify-between impulse-items-center impulse-mb-4">
        <Heading level={2} UNSAFE_style={{ margin: 0 }}>Needs Attention</Heading>
        <Badge variant="negative" size="S">{residents?.length || 0}</Badge>
      </div>
      <Divider />

      <div className="impulse-flex impulse-flex-col impulse-gap-4 impulse-mt-4">
        {residents?.map((resident) => (
          <Link key={resident.id} to={getResidentPath(resident.id)} className="attention-card">
            <div className="impulse-flex impulse-justify-between impulse-items-start">
              <div className="impulse-flex impulse-flex-col impulse-gap-1">
                <Text UNSAFE_className="impulse-value">
                  {resident.firstName} {resident.lastName}
                </Text>
                <Text UNSAFE_className="impulse-muted-sm">Room {resident.roomNumber}</Text>
              </div>
              <div className="impulse-flex impulse-flex-col impulse-items-end impulse-gap-1">
                {resident.hasOverdueMedications ? (
                  <Badge variant="negative" size="S">Overdue Med</Badge>
                ) : (
                  <Badge variant="informative" size="S">{resident.activeMedicationsCount} meds</Badge>
                )}
                <Text UNSAFE_className="impulse-muted-xs">
                  {resident.openCareGoals} open goals
                </Text>
              </div>
            </div>
            {resident.nextMedicationDue && (
              <div className="impulse-flex impulse-items-center impulse-gap-2 impulse-mt-2">
                <StatusLight variant={resident.hasOverdueMedications ? 'negative' : 'notice'}>
                  Next med: {formatTime(resident.nextMedicationDue)}
                </StatusLight>
              </div>
            )}
          </Link>
        ))}
      </div>
    </Card>
  );
}

// ========================================
// Upcoming Tasks Card
// ========================================

function UpcomingTasksCard({ tasks }: { tasks?: readonly UpcomingTask[] }) {
  const getTaskVariant = (priority: string, isOverdue: boolean) => {
    if (isOverdue) return 'negative';
    switch (priority) {
      case 'urgent': return 'negative';
      case 'high': return 'notice';
      default: return 'neutral';
    }
  };

  const getTaskIcon = (type: string) => {
    switch (type) {
      case 'medication': return 'pill';
      case 'assessment': return 'clipboard';
      case 'care_review': return 'heart';
      default: return 'task';
    }
  };

  return (
    <Card UNSAFE_className="impulse-card">
      <div className="impulse-flex impulse-justify-between impulse-items-center impulse-mb-4">
        <Heading level={2} UNSAFE_style={{ margin: 0 }}>Upcoming Tasks</Heading>
        <ActionButton>View All</ActionButton>
      </div>
      <Divider />

      <div className="impulse-flex impulse-flex-col impulse-gap-3 impulse-mt-4">
        {tasks?.slice(0, 5).map((task) => (
          <div key={task.id} className={`task-item ${task.isOverdue ? 'task-overdue' : ''}`}>
            <div className="impulse-flex impulse-justify-between impulse-items-start">
              <div className="impulse-flex impulse-flex-col impulse-gap-1">
                <div className="impulse-flex impulse-items-center impulse-gap-2">
                  <Text UNSAFE_className="impulse-value">{task.title}</Text>
                  {task.isOverdue && <Badge variant="negative" size="S">Overdue</Badge>}
                </div>
                <Text UNSAFE_className="impulse-muted-sm">{task.description}</Text>
                <Link to={getResidentPath(task.residentId)}>
                  <Text UNSAFE_className="impulse-link">{task.residentName}</Text>
                </Link>
              </div>
              <Badge variant={getTaskVariant(task.priority, task.isOverdue)} size="S">
                {formatTime(task.dueAt)}
              </Badge>
            </div>
          </div>
        ))}
      </div>
    </Card>
  );
}

// ========================================
// Compliance Card
// ========================================

function ComplianceCard({ compliance }: { compliance: MedicationComplianceData }) {
  return (
    <Card UNSAFE_className="impulse-card">
      <Heading level={2} UNSAFE_style={{ margin: 0, marginBottom: 16 }}>Medication Compliance</Heading>
      <Divider />

      <div className="impulse-flex impulse-flex-col impulse-gap-4 impulse-mt-4">
        {/* Compliance Meter */}
        <div className="impulse-flex impulse-items-center impulse-gap-4">
          <div className="impulse-progress-ring">
            <svg width="120" height="120">
              <circle className="impulse-progress-ring-bg" cx="60" cy="60" r="52" />
              <circle
                className="impulse-progress-ring-fill"
                cx="60" cy="60" r="52"
                strokeDasharray={`${compliance.complianceRate * 3.27} 327`}
              />
            </svg>
            <div className="impulse-progress-ring-center">
              <Text UNSAFE_className="impulse-stat-lg">{compliance.complianceRate}%</Text>
              <Text UNSAFE_className="impulse-muted-xs">Compliance</Text>
            </div>
          </div>

          <div className="impulse-flex impulse-flex-col impulse-gap-2 impulse-flex-1">
            <div className="impulse-flex impulse-justify-between">
              <Text UNSAFE_className="impulse-muted-sm">On Time</Text>
              <Text UNSAFE_className="impulse-success">{compliance.onTimeAdministrations}</Text>
            </div>
            <div className="impulse-flex impulse-justify-between">
              <Text UNSAFE_className="impulse-muted-sm">Late</Text>
              <Text UNSAFE_className="impulse-stat-highlight">{compliance.lateAdministrations}</Text>
            </div>
            <div className="impulse-flex impulse-justify-between">
              <Text UNSAFE_className="impulse-muted-sm">Refused</Text>
              <Text>{compliance.refusedAdministrations}</Text>
            </div>
            <Divider />
            <div className="impulse-flex impulse-justify-between">
              <Text UNSAFE_className="impulse-muted-sm">Total</Text>
              <Text UNSAFE_className="impulse-value">{compliance.totalAdministrations}</Text>
            </div>
          </div>
        </div>
      </div>
    </Card>
  );
}

// ========================================
// Schedule Card
// ========================================

function ScheduleCard({ schedule }: { schedule?: readonly DailyScheduleItem[] }) {
  const getStatusVariant = (status: string): 'positive' | 'neutral' | 'negative' => {
    switch (status) {
      case 'completed': return 'positive';
      case 'pending': return 'neutral';
      case 'missed': return 'negative';
      default: return 'neutral';
    }
  };

  return (
    <Card UNSAFE_className="impulse-card">
      <div className="impulse-flex impulse-justify-between impulse-items-center impulse-mb-4">
        <Heading level={2} UNSAFE_style={{ margin: 0 }}>Today's Schedule</Heading>
        <Badge variant="informative" size="S">
          {schedule?.filter(s => s.status === 'completed').length}/{schedule?.length}
        </Badge>
      </div>
      <Divider />

      <div className="schedule-timeline impulse-mt-4">
        {schedule?.map((item) => (
          <div key={item.id} className={`schedule-item schedule-${item.status}`}>
            <div className="schedule-time">
              <Text UNSAFE_className="impulse-muted-sm">{item.time}</Text>
            </div>
            <div className="schedule-marker">
              <div className={`schedule-dot schedule-dot-${item.status}`} />
            </div>
            <div className="schedule-content">
              <div className="impulse-flex impulse-items-center impulse-gap-2">
                <Text UNSAFE_className="impulse-value">{item.title}</Text>
                <StatusLight variant={getStatusVariant(item.status)}>
                  {item.status}
                </StatusLight>
              </div>
              <Text UNSAFE_className="impulse-muted-sm">{item.description}</Text>
            </div>
          </div>
        ))}
      </div>
    </Card>
  );
}

// ========================================
// Recent Activity Card
// ========================================

function RecentActivityCard({ activities }: { activities?: readonly RecentActivity[] }) {
  const getActivityVariant = (type: string): 'positive' | 'informative' | 'notice' | 'neutral' => {
    switch (type) {
      case 'medication_given': return 'positive';
      case 'assessment_completed': return 'informative';
      case 'care_plan_updated': return 'notice';
      case 'resident_admitted': return 'positive';
      default: return 'neutral';
    }
  };

  return (
    <Card UNSAFE_className="impulse-card">
      <div className="impulse-flex impulse-justify-between impulse-items-center impulse-mb-4">
        <Heading level={2} UNSAFE_style={{ margin: 0 }}>Recent Activity</Heading>
        <ActionButton>View All</ActionButton>
      </div>
      <Divider />

      <div className="activity-feed impulse-mt-4">
        {activities?.slice(0, 5).map((activity) => (
          <div key={activity.id} className="activity-item">
            <div className="impulse-flex impulse-justify-between impulse-items-start">
              <div className="impulse-flex impulse-flex-col impulse-gap-1">
                <div className="impulse-flex impulse-items-center impulse-gap-2">
                  <Badge variant={getActivityVariant(activity.type)} size="S">
                    {activity.type.replace('_', ' ')}
                  </Badge>
                  <Text UNSAFE_className="impulse-muted-xs">
                    {formatRelativeTime(activity.timestamp)}
                  </Text>
                </div>
                <Text UNSAFE_className="impulse-value">{activity.title}</Text>
                <Text UNSAFE_className="impulse-muted-sm">{activity.description}</Text>
                <div className="impulse-flex impulse-gap-2">
                  <Link to={getResidentPath(activity.residentId)}>
                    <Text UNSAFE_className="impulse-link">{activity.residentName}</Text>
                  </Link>
                  <Text UNSAFE_className="impulse-muted-xs">by {activity.performedBy}</Text>
                </div>
              </div>
            </div>
          </div>
        ))}
      </div>
    </Card>
  );
}
