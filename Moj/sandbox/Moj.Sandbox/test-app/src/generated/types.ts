/**
 * Auto-generated from C# domain model by Moj.TanStack.DSL
 * Source: Moj.Sandbox.DomainModels
 */

// ============= Enums (as union types) =============

export type IncidentStatus = 'open' | 'inProgress' | 'onHold' | 'resolved' | 'closed';

export type IncidentPriority = 'critical' | 'high' | 'medium' | 'low';

export type IncidentCategory = 'bug' | 'feature' | 'support' | 'security' | 'performance';

// ============= Domain Types =============

export interface User {
  id: string;
  name: string;
  email: string;
  avatarUrl?: string;
  department: string;
  roles: string[];
}

export interface CommentReaction {
  userId: string;
  emoji: string;
  createdAt: string;
}

export interface Attachment {
  id: string;
  fileName: string;
  contentType: string;
  size: number;
  url: string;
  uploadedAt: string;
}

export interface Comment {
  id: string;
  text: string;
  author: User;
  createdAt: string;
  editedAt?: string;
  attachments: Attachment[];
  reactions: CommentReaction[];
}

export interface IncidentMetadata {
  createdAt: string;
  updatedAt: string;
  resolvedAt?: string;
  closedAt?: string;
  createdBy: string;
  viewCount: number;
  timeToFirstResponse?: string;
  timeToResolution?: string;
}

export interface RelatedIncident {
  id: string;
  title: string;
  status: IncidentStatus;
  relationType: string;
}

export interface CustomField {
  key: string;
  label: string;
  value?: unknown;
  fieldType: string;
}

export interface SlaMetrics {
  timeToResponse?: string;
  timeToResolution?: string;
  responsePercentage: number;
  resolutionPercentage: number;
}

export interface SlaInfo {
  policyId: string;
  policyName: string;
  responseDueAt?: string;
  resolutionDueAt?: string;
  responseBreached: boolean;
  resolutionBreached: boolean;
  metrics: SlaMetrics;
}

export interface Incident {
  id: string;
  title: string;
  description: string;
  status: IncidentStatus;
  priority: IncidentPriority;
  category: IncidentCategory;
  assignedTo?: User;
  reporter?: User;
  watchers: User[];
  labels: string[];
  comments: Comment[];
  attachments: Attachment[];
  relatedIncidents: RelatedIncident[];
  customFields: Record<string, CustomField>;
  metadata: IncidentMetadata;
  sla?: SlaInfo;
}

// ============= UI & Filter Types =============

export interface IncidentFilters {
  statuses: IncidentStatus[];
  priorities: IncidentPriority[];
  categories: IncidentCategory[];
  assigneeId?: string;
  searchQuery?: string;
  dateFrom?: string;
  dateTo?: string;
  labels: string[];
}

export interface SortConfig {
  field: string;
  direction: 'asc' | 'desc';
}

export interface PaginationState {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface UiState {
  sidebarOpen: boolean;
  activeView: 'list' | 'board' | 'timeline';
  sort: SortConfig;
  pagination: PaginationState;
}

// ============= App State =============

export interface IncidentAppState {
  incidents: Incident[];
  selectedIncident?: Incident;
  currentUser?: User;
  isLoading: boolean;
  error?: string;
  filters: IncidentFilters;
  ui: UiState;
}
