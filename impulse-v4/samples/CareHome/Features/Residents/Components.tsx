import { useEffect, useState } from 'react';
import {
  View,
  Heading,
  Text,
  Button,
  Badge,
} from '@adobe/react-spectrum';
import { useImpulse } from '../../client/shared/ImpulseProvider';
import type { ListResidentsResponse, ResidentSummary } from '../../generated/types';
import { RoutePaths } from '../../generated/routePaths';

// ========================================
// Residents List - Vertical Slice Component
// Fetches data via Impulse, renders with S2
// ========================================

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
    return <Text>Loading residents...</Text>;
  }

  if (!data) {
    return <Text>Failed to load residents</Text>;
  }

  return (
    <div className="residents-page" data-testid="residents-list">
      <header className="page-header">
        <Heading level={1}>Residents</Heading>
        <a href="/admission/wizard">
          <Button variant="accent">+ New Admission</Button>
        </a>
      </header>

      <View UNSAFE_className="residents-card" backgroundColor="gray-50" padding="size-400" borderRadius="medium">
        <div className="card-header">
          <Heading level={2}>All Residents ({data.totalCount})</Heading>
        </div>

        <div className="residents-list" data-testid="residents-table">
          {data.residents.map((resident) => (
            <a key={resident.id} href={`/residents/${resident.id}`} className="resident-row">
              <div className="resident-name">
                {resident.firstName} {resident.lastName}
              </div>
              <div className="resident-room">{resident.roomNumber}</div>
              <div className="resident-care">
                <CareLevelBadge level={resident.careLevel} />
              </div>
              <div className="resident-age">{resident.age}</div>
              <div className="resident-status">
                <StatusBadge status={resident.status} />
              </div>
            </a>
          ))}
        </div>
      </View>
    </div>
  );
}

// ========================================
// Resident Detail
// ========================================

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
    return <Text>Loading...</Text>;
  }

  if (!resident) {
    return <Text>Resident not found</Text>;
  }

  return (
    <div className="resident-detail">
      <header className="page-header">
        <a href="/residents">← Back to Residents</a>
        <Heading level={1}>{resident.firstName} {resident.lastName}</Heading>
      </header>

      <View backgroundColor="gray-50" padding="size-400" borderRadius="medium">
        <div className="detail-grid">
          <div className="detail-item">
            <Text UNSAFE_className="label">Room</Text>
            <Text UNSAFE_className="value">{resident.roomNumber}</Text>
          </div>
          <div className="detail-item">
            <Text UNSAFE_className="label">Care Level</Text>
            <CareLevelBadge level={resident.careLevel} />
          </div>
          <div className="detail-item">
            <Text UNSAFE_className="label">Age</Text>
            <Text UNSAFE_className="value">{resident.age}</Text>
          </div>
          <div className="detail-item">
            <Text UNSAFE_className="label">Status</Text>
            <StatusBadge status={resident.status} />
          </div>
        </div>
      </View>
    </div>
  );
}

// ========================================
// Helper Components
// ========================================

function CareLevelBadge({ level }: { level: string }) {
  const variant = {
    Independent: 'positive',
    Assisted: 'info',
    FullCare: 'yellow',
    Memory: 'negative',
  }[level] as 'positive' | 'info' | 'yellow' | 'negative' || 'neutral';

  return <Badge variant={variant}>{level}</Badge>;
}

function StatusBadge({ status }: { status: string }) {
  const variant = status === 'Active' ? 'positive' : 'neutral';
  return <Badge variant={variant}>{status}</Badge>;
}
