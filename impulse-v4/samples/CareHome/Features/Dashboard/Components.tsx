import { useEffect, useState } from 'react';
import {
  View,
  Heading,
  Text,
  Button,
  Badge,
} from '@adobe/react-spectrum';
import { useImpulse } from '../../client/shared/ImpulseProvider';
import type { GetDashboardResponse } from '../../generated/types';

// ========================================
// Dashboard - Vertical Slice Component
// ========================================

export function Dashboard() {
  const ctx = useImpulse();
  const [data, setData] = useState<GetDashboardResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    async function load() {
      try {
        const response = await ctx.impulse<GetDashboardResponse>('/');
        setData(response);
      } catch (err) {
        console.error('Failed to load dashboard:', err);
      } finally {
        setIsLoading(false);
      }
    }
    load();
  }, [ctx]);

  if (isLoading) {
    return <Text>Loading dashboard...</Text>;
  }

  if (!data) {
    return <Text>Failed to load dashboard</Text>;
  }

  return (
    <div className="dashboard-page" data-testid="dashboard">
      <header className="page-header">
        <Heading level={1}>Dashboard</Heading>
        <Text>Green Valley Care Home Overview</Text>
      </header>

      <div className="stats-grid">
        <StatCard
          title="Total Residents"
          value={data.stats.totalResidents}
          variant="info"
        />
        <StatCard
          title="Active"
          value={data.stats.activeResidents}
          variant="positive"
        />
        <StatCard
          title="New This Month"
          value={data.stats.newAdmissionsThisMonth}
          variant="yellow"
        />
        <StatCard
          title="Upcoming Birthdays"
          value={data.stats.upcomingBirthdays}
          variant="info"
        />
      </div>

      <div className="dashboard-grid">
        <View UNSAFE_className="dashboard-card" backgroundColor="gray-50" padding="size-400" borderRadius="medium">
          <div className="card-header">
            <Heading level={2}>Recent Admissions</Heading>
            <a href="/residents">
              <Button variant="secondary">View All</Button>
            </a>
          </div>
          <div className="recent-list">
            {data.recentAdmissions.map((resident) => (
              <div key={resident.id} className="recent-item">
                <Text UNSAFE_className="name">
                  {resident.firstName} {resident.lastName}
                </Text>
                <Text UNSAFE_className="room">Room {resident.roomNumber}</Text>
                <Badge variant="positive">{resident.careLevel}</Badge>
              </div>
            ))}
          </div>
        </View>

        <View UNSAFE_className="dashboard-card" backgroundColor="gray-50" padding="size-400" borderRadius="medium">
          <div className="card-header">
            <Heading level={2}>Quick Actions</Heading>
          </div>
          <div className="quick-actions">
            <a href="/admission/wizard">
              <Button variant="accent" UNSAFE_className="action-btn">
                + New Admission
              </Button>
            </a>
            <a href="/residents">
              <Button variant="secondary" UNSAFE_className="action-btn">
                View Residents
              </Button>
            </a>
          </div>
        </View>
      </div>
    </div>
  );
}

// ========================================
// Helper Components
// ========================================

interface StatCardProps {
  title: string;
  value: number;
  variant: 'positive' | 'info' | 'yellow' | 'negative';
}

function StatCard({ title, value, variant }: StatCardProps) {
  return (
    <View UNSAFE_className="stat-card" backgroundColor="gray-50" padding="size-300" borderRadius="medium">
      <Badge variant={variant} UNSAFE_className="stat-badge">
        {value}
      </Badge>
      <Text UNSAFE_className="stat-title">{title}</Text>
    </View>
  );
}
