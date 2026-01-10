import React from 'react';
import { Link } from '@tanstack/react-router';
import {
  Card,
  CardPreview,
  Badge,
  Button,
  Heading,
  Text,
  Divider,
  StatusLight,
  Meter,
  ProgressBar,
  ActionButton,
  Avatar,
  Content,
  Header,
  Footer,
  IllustratedMessage,
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
// Server-driven UI with S2 design system
// ========================================

interface DashboardProps {
  data: GetDashboardResponse;
}

export function Dashboard({ data }: DashboardProps) {
  return (
    <div className="dashboard-container">
      {/* Page Header */}
      <header className="dashboard-header">
        <div className="dashboard-header-content">
          <Heading level={1} UNSAFE_className="dashboard-title">Dashboard</Heading>
          <Text slot="description" UNSAFE_className="dashboard-subtitle">
            {new Date().toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' })}
          </Text>
        </div>
        <div className="dashboard-actions">
          <Button variant="secondary" fillStyle="outline">Export Report</Button>
          <Button variant="accent">+ New Resident</Button>
        </div>
      </header>

      {/* Stats Row */}
      <section className="stats-section">
        <StatCard
          icon="👥"
          label="Total Residents"
          value={data.stats.totalResidents}
          detail={`${data.stats.activeResidents} active`}
          color="blue"
        />
        <StatCard
          icon="💊"
          label="Medications Due"
          value={data.stats.medicationsDueToday}
          detail={data.stats.overdueMedications > 0 ? `${data.stats.overdueMedications} overdue` : 'All on track'}
          color={data.stats.overdueMedications > 0 ? 'red' : 'green'}
        />
        <StatCard
          icon="📋"
          label="Care Plans"
          value={data.stats.activeCarePlans}
          detail={`${data.stats.upcomingAssessments} reviews due`}
          color="purple"
        />
        <StatCard
          icon="🏥"
          label="Hospitalized"
          value={data.stats.hospitalizedResidents}
          detail={data.stats.onLeaveResidents > 0 ? `${data.stats.onLeaveResidents} on leave` : 'None on leave'}
          color={data.stats.hospitalizedResidents > 0 ? 'orange' : 'gray'}
        />
      </section>

      {/* Main Grid */}
      <div className="dashboard-main">
        {/* Primary Content */}
        <div className="dashboard-primary">
          <AlertsCard residents={data.residentsNeedingAttention} />
          <TasksCard tasks={data.upcomingTasks} />
        </div>

        {/* Secondary Content */}
        <div className="dashboard-secondary">
          <ComplianceCard compliance={data.medicationCompliance} />
          <ScheduleCard schedule={data.todaysSchedule} />
          <ActivityCard activities={data.recentActivities} />
        </div>
      </div>
    </div>
  );
}

// ========================================
// Stat Card - Clean metric display
// ========================================

function StatCard({ icon, label, value, detail, color }: {
  icon: string;
  label: string;
  value: number;
  detail: string;
  color: 'blue' | 'green' | 'red' | 'orange' | 'purple' | 'gray';
}) {
  const colorMap = {
    blue: { bg: '#eff6ff', accent: '#2563eb', text: '#1e40af' },
    green: { bg: '#f0fdf4', accent: '#16a34a', text: '#166534' },
    red: { bg: '#fef2f2', accent: '#dc2626', text: '#991b1b' },
    orange: { bg: '#fff7ed', accent: '#ea580c', text: '#9a3412' },
    purple: { bg: '#faf5ff', accent: '#9333ea', text: '#6b21a8' },
    gray: { bg: '#f9fafb', accent: '#6b7280', text: '#374151' },
  };
  const colors = colorMap[color];

  return (
    <div className="stat-card" style={{ backgroundColor: colors.bg, borderLeft: `4px solid ${colors.accent}` }}>
      <div className="stat-icon">{icon}</div>
      <div className="stat-content">
        <span className="stat-label">{label}</span>
        <span className="stat-value" style={{ color: colors.accent }}>{value}</span>
        <span className="stat-detail" style={{ color: colors.text }}>{detail}</span>
      </div>
    </div>
  );
}

// ========================================
// Alerts Card - Residents needing attention
// ========================================

function AlertsCard({ residents }: { residents?: readonly ResidentQuickView[] }) {
  return (
    <Card UNSAFE_className="dashboard-card alerts-card">
      <div className="card-header">
        <div className="card-title-group">
          <span className="card-icon">⚠️</span>
          <Heading level={3} UNSAFE_className="card-title">Needs Attention</Heading>
        </div>
        <Badge variant="negative" size="S">{residents?.length || 0}</Badge>
      </div>

      <div className="alert-list">
        {residents?.slice(0, 6).map((resident) => (
          <Link key={resident.id} to={getResidentPath(resident.id)} className="alert-item">
            <Avatar
              src={`https://api.dicebear.com/7.x/initials/svg?seed=${resident.firstName}%20${resident.lastName}&backgroundColor=fef3c7`}
              alt={`${resident.firstName} ${resident.lastName}`}
              size="M"
            />
            <div className="alert-content">
              <span className="alert-name">{resident.firstName} {resident.lastName}</span>
              <span className="alert-room">Room {resident.roomNumber}</span>
            </div>
            <div className="alert-status">
              {resident.hasOverdueMedications ? (
                <Badge variant="negative" size="S">Overdue</Badge>
              ) : (
                <Badge variant="notice" size="S">{resident.activeMedicationsCount} meds</Badge>
              )}
              {resident.nextMedicationDue && (
                <span className="alert-time">Next: {formatTime(resident.nextMedicationDue)}</span>
              )}
            </div>
          </Link>
        ))}
      </div>

      {(!residents || residents.length === 0) && (
        <div className="empty-state">
          <span className="empty-icon">✅</span>
          <span>All residents are on track</span>
        </div>
      )}
    </Card>
  );
}

// ========================================
// Tasks Card - Upcoming tasks
// ========================================

function TasksCard({ tasks }: { tasks?: readonly UpcomingTask[] }) {
  return (
    <Card UNSAFE_className="dashboard-card tasks-card">
      <div className="card-header">
        <div className="card-title-group">
          <span className="card-icon">📝</span>
          <Heading level={3} UNSAFE_className="card-title">Upcoming Tasks</Heading>
        </div>
        <Button variant="secondary" fillStyle="outline" size="S">View All</Button>
      </div>

      <div className="task-list">
        {tasks?.slice(0, 6).map((task) => (
          <div key={task.id} className={`task-item ${task.isOverdue ? 'task-overdue' : ''}`}>
            <div className="task-time">
              <span className="task-hour">{formatTime(task.dueAt)}</span>
            </div>
            <div className="task-content">
              <div className="task-header">
                <span className="task-title">{task.title}</span>
                {task.isOverdue && <Badge variant="negative" size="S">Overdue</Badge>}
              </div>
              <span className="task-description">{task.description}</span>
              <Link to={getResidentPath(task.residentId)} className="task-resident">
                {task.residentName}
              </Link>
            </div>
          </div>
        ))}
      </div>
    </Card>
  );
}

// ========================================
// Compliance Card - Medication compliance
// ========================================

function ComplianceCard({ compliance }: { compliance: MedicationComplianceData }) {
  const rate = compliance.complianceRate;
  const rateColor = rate >= 90 ? '#16a34a' : rate >= 75 ? '#ea580c' : '#dc2626';

  return (
    <Card UNSAFE_className="dashboard-card compliance-card">
      <div className="card-header">
        <div className="card-title-group">
          <span className="card-icon">📊</span>
          <Heading level={3} UNSAFE_className="card-title">Compliance</Heading>
        </div>
      </div>

      <div className="compliance-content">
        <div className="compliance-ring">
          <svg viewBox="0 0 100 100" className="compliance-svg">
            <circle cx="50" cy="50" r="40" className="compliance-bg" />
            <circle
              cx="50" cy="50" r="40"
              className="compliance-fill"
              style={{
                stroke: rateColor,
                strokeDasharray: `${rate * 2.51} 251`
              }}
            />
          </svg>
          <div className="compliance-value">
            <span className="compliance-percent" style={{ color: rateColor }}>{rate}%</span>
            <span className="compliance-label">compliance</span>
          </div>
        </div>

        <div className="compliance-stats">
          <div className="compliance-stat">
            <span className="compliance-stat-value compliance-success">{compliance.onTimeAdministrations}</span>
            <span className="compliance-stat-label">On Time</span>
          </div>
          <div className="compliance-stat">
            <span className="compliance-stat-value compliance-warning">{compliance.lateAdministrations}</span>
            <span className="compliance-stat-label">Late</span>
          </div>
          <div className="compliance-stat">
            <span className="compliance-stat-value compliance-danger">{compliance.refusedAdministrations}</span>
            <span className="compliance-stat-label">Refused</span>
          </div>
        </div>
      </div>
    </Card>
  );
}

// ========================================
// Schedule Card - Today's schedule
// ========================================

function ScheduleCard({ schedule }: { schedule?: readonly DailyScheduleItem[] }) {
  const completed = schedule?.filter(s => s.status === 'completed').length || 0;
  const total = schedule?.length || 0;

  return (
    <Card UNSAFE_className="dashboard-card schedule-card">
      <div className="card-header">
        <div className="card-title-group">
          <span className="card-icon">🗓️</span>
          <Heading level={3} UNSAFE_className="card-title">Today's Schedule</Heading>
        </div>
        <Badge variant="informative" size="S">{completed}/{total}</Badge>
      </div>

      <div className="schedule-list">
        {schedule?.slice(0, 6).map((item, index) => (
          <div key={item.id} className={`schedule-item schedule-${item.status}`}>
            <div className="schedule-time">{item.time}</div>
            <div className="schedule-indicator">
              <div className={`schedule-dot schedule-dot-${item.status}`} />
              {index < (schedule?.length || 0) - 1 && <div className="schedule-line" />}
            </div>
            <div className="schedule-content">
              <span className="schedule-title">{item.title}</span>
              <span className="schedule-desc">{item.description}</span>
            </div>
          </div>
        ))}
      </div>
    </Card>
  );
}

// ========================================
// Activity Card - Recent activity
// ========================================

function ActivityCard({ activities }: { activities?: readonly RecentActivity[] }) {
  const getTypeIcon = (type: string) => {
    switch (type) {
      case 'medication_given': return '💊';
      case 'assessment_completed': return '📋';
      case 'care_plan_updated': return '📝';
      case 'resident_admitted': return '🏠';
      default: return '📌';
    }
  };

  return (
    <Card UNSAFE_className="dashboard-card activity-card">
      <div className="card-header">
        <div className="card-title-group">
          <span className="card-icon">🕐</span>
          <Heading level={3} UNSAFE_className="card-title">Recent Activity</Heading>
        </div>
        <Button variant="secondary" fillStyle="outline" size="S">View All</Button>
      </div>

      <div className="activity-list">
        {activities?.slice(0, 5).map((activity) => (
          <div key={activity.id} className="activity-item">
            <span className="activity-icon">{getTypeIcon(activity.type)}</span>
            <div className="activity-content">
              <span className="activity-title">{activity.title}</span>
              <span className="activity-desc">{activity.description}</span>
              <div className="activity-meta">
                <Link to={getResidentPath(activity.residentId)} className="activity-resident">
                  {activity.residentName}
                </Link>
                <span className="activity-time">{formatRelativeTime(activity.timestamp)}</span>
              </div>
            </div>
          </div>
        ))}
      </div>
    </Card>
  );
}
