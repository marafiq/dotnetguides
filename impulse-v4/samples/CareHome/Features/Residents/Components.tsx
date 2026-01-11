import { useEffect, useState } from 'react';
import { Link } from '@tanstack/react-router';
import { UserPlus, ArrowLeft, User, Home as HomeIcon, Heart, Calendar } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '../../client/components/ui/card';
import { Button } from '../../client/components/ui/button';
import { Badge } from '../../client/components/ui/badge';
import { useImpulse } from '../../client/shared/ImpulseProvider';
import type { ListResidentsResponse, ResidentSummary } from '../../generated/types';
import { RoutePaths } from '../../generated/routePaths';

export function ResidentsList() {
  const ctx = useImpulse();
  const [data, setData] = useState<ListResidentsResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    async function load() {
      try {
        const response = await ctx.impulse<ListResidentsResponse>(RoutePaths.ListResidents);
        setData(response);
      } catch (err) {
        console.error('Failed to load residents:', err);
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
        <p className="text-muted-foreground">Failed to load residents</p>
      </div>
    );
  }

  return (
    <div data-testid="residents-list" className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Residents</h1>
          <p className="text-muted-foreground">{data.totalCount} total residents</p>
        </div>
        <Link to="/admission/wizard">
          <Button>
            <UserPlus className="mr-2 h-4 w-4" />
            New Admission
          </Button>
        </Link>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>All Residents</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="divide-y" data-testid="residents-table">
            {data.residents.map((resident) => (
              <Link
                key={resident.id}
                to={`/residents/${resident.id}` as '/residents/$id'}
                params={{ id: String(resident.id) }}
                className="flex items-center justify-between py-4 hover:bg-slate-50 -mx-6 px-6 transition-colors"
              >
                <div className="flex items-center gap-4">
                  <div className="h-10 w-10 rounded-full bg-primary/10 flex items-center justify-center">
                    <User className="h-5 w-5 text-primary" />
                  </div>
                  <div>
                    <p className="font-medium">{resident.firstName} {resident.lastName}</p>
                    <p className="text-sm text-muted-foreground">Room {resident.roomNumber}</p>
                  </div>
                </div>
                <div className="flex items-center gap-4">
                  <CareLevelBadge level={resident.careLevel} />
                  <span className="text-sm text-muted-foreground w-16">{resident.age} years</span>
                  <StatusBadge status={resident.status} />
                </div>
              </Link>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

interface ResidentDetailProps {
  id: number;
}

export function ResidentDetail({ id }: ResidentDetailProps) {
  const ctx = useImpulse();
  const [resident, setResident] = useState<ResidentSummary | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    async function load() {
      try {
        const response = await ctx.impulse<ResidentSummary>(`/residents/${id}`);
        setResident(response);
      } catch (err) {
        console.error('Failed to load resident:', err);
      } finally {
        setIsLoading(false);
      }
    }
    load();
  }, [ctx, id]);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    );
  }

  if (!resident) {
    return (
      <div className="text-center py-12">
        <p className="text-muted-foreground">Resident not found</p>
      </div>
    );
  }

  return (
    <div className="space-y-6 max-w-3xl">
      <div className="flex items-center gap-4">
        <Link to="/residents">
          <Button variant="ghost" size="sm">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to Residents
          </Button>
        </Link>
      </div>

      <div className="flex items-center gap-4">
        <div className="h-16 w-16 rounded-full bg-primary/10 flex items-center justify-center">
          <User className="h-8 w-8 text-primary" />
        </div>
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{resident.firstName} {resident.lastName}</h1>
          <StatusBadge status={resident.status} />
        </div>
      </div>

      <Card>
        <CardContent className="pt-6">
          <div className="grid gap-6 md:grid-cols-2">
            <DetailItem icon={<HomeIcon className="h-4 w-4" />} label="Room" value={resident.roomNumber} />
            <DetailItem icon={<Heart className="h-4 w-4" />} label="Care Level" value={<CareLevelBadge level={resident.careLevel} />} />
            <DetailItem icon={<Calendar className="h-4 w-4" />} label="Age" value={`${resident.age} years`} />
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

function DetailItem({ icon, label, value }: { icon: React.ReactNode; label: string; value: React.ReactNode }) {
  return (
    <div className="flex items-start gap-3">
      <div className="h-8 w-8 rounded-md bg-slate-100 flex items-center justify-center text-slate-500">
        {icon}
      </div>
      <div>
        <p className="text-sm text-muted-foreground">{label}</p>
        <div className="font-medium">{value}</div>
      </div>
    </div>
  );
}

function CareLevelBadge({ level }: { level: string }) {
  const variant = {
    Independent: 'success',
    Assisted: 'info',
    FullCare: 'warning',
    Memory: 'destructive',
  }[level] as 'success' | 'info' | 'warning' | 'destructive' || 'secondary';

  return <Badge variant={variant}>{level}</Badge>;
}

function StatusBadge({ status }: { status: string }) {
  const variant = status === 'Active' ? 'success' : 'secondary';
  return <Badge variant={variant}>{status}</Badge>;
}
