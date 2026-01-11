import { useEffect, useState } from 'react';
import { Link } from '@tanstack/react-router';
import { Users, UserPlus, Calendar, Activity } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '../../client/components/ui/card';
import { Button } from '../../client/components/ui/button';
import { Badge } from '../../client/components/ui/badge';
import { useImpulse } from '../../client/shared/ImpulseProvider';
import type { GetDashboardResponse } from '../../generated/types';

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
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    );
  }

  if (!data) {
    return (
      <div className="text-center py-12">
        <p className="text-muted-foreground">Failed to load dashboard</p>
      </div>
    );
  }

  return (
    <div data-testid="dashboard" className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Dashboard</h1>
        <p className="text-muted-foreground">Green Valley Care Home Overview</p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <StatCard
          title="Total Residents"
          value={data.stats.totalResidents}
          icon={<Users className="h-4 w-4 text-muted-foreground" />}
        />
        <StatCard
          title="Active"
          value={data.stats.activeResidents}
          icon={<Activity className="h-4 w-4 text-green-500" />}
          trend="positive"
        />
        <StatCard
          title="New This Month"
          value={data.stats.newAdmissionsThisMonth}
          icon={<UserPlus className="h-4 w-4 text-blue-500" />}
        />
        <StatCard
          title="Upcoming Birthdays"
          value={data.stats.upcomingBirthdays}
          icon={<Calendar className="h-4 w-4 text-purple-500" />}
        />
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-base font-medium">Recent Admissions</CardTitle>
            <Link to="/residents">
              <Button variant="ghost" size="sm">View All</Button>
            </Link>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {data.recentAdmissions.map((resident) => (
                <div key={resident.id} className="flex items-center justify-between py-2 border-b last:border-0">
                  <div>
                    <p className="font-medium">{resident.firstName} {resident.lastName}</p>
                    <p className="text-sm text-muted-foreground">Room {resident.roomNumber}</p>
                  </div>
                  <Badge variant="success">{resident.careLevel}</Badge>
                </div>
              ))}
              {data.recentAdmissions.length === 0 && (
                <p className="text-sm text-muted-foreground text-center py-4">No recent admissions</p>
              )}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base font-medium">Quick Actions</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <Link to="/admission/wizard" className="block">
              <Button className="w-full justify-start" size="lg">
                <UserPlus className="mr-2 h-4 w-4" />
                New Admission
              </Button>
            </Link>
            <Link to="/residents" className="block">
              <Button variant="outline" className="w-full justify-start" size="lg">
                <Users className="mr-2 h-4 w-4" />
                View Residents
              </Button>
            </Link>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

interface StatCardProps {
  title: string;
  value: number;
  icon: React.ReactNode;
  trend?: 'positive' | 'negative';
}

function StatCard({ title, value, icon, trend }: StatCardProps) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
        {icon}
      </CardHeader>
      <CardContent>
        <div className="text-3xl font-bold">{value}</div>
        {trend && (
          <p className={`text-xs ${trend === 'positive' ? 'text-green-500' : 'text-red-500'}`}>
            {trend === 'positive' ? '+' : '-'}12% from last month
          </p>
        )}
      </CardContent>
    </Card>
  );
}
