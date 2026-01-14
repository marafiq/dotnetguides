export type IncidentStatus = 'open' | 'inProgress' | 'onHold' | 'resolved' | 'closed';

export type IncidentPriority = 'critical' | 'high' | 'medium' | 'low';

export type IncidentCategory = 'bug' | 'feature' | 'support' | 'security' | 'performance';

export interface User {
  id?: string;
  name?: string;
  email?: string;
  avatarUrl?: string;
  department?: string;
  roles?: string[];
}

export interface Comment {
  id?: string;
  text?: string;
  author?: User;
  createdAt: string;
  editedAt?: string | null;
  attachments?: Attachment[];
  reactions?: CommentReaction[];
}

export interface Attachment {
  id?: string;
  fileName?: string;
  contentType?: string;
  size: number;
  url?: string;
  uploadedAt: string;
}

export interface IncidentMetadata {
  createdAt: string;
  updatedAt: string;
  resolvedAt?: string | null;
  closedAt?: string | null;
  createdBy?: string;
  viewCount: number;
  timeToFirstResponse?: TimeSpan | null;
  timeToResolution?: TimeSpan | null;
}

export interface Incident {
  id?: string;
  title?: string;
  description?: string;
  status: 'open' | 'inProgress' | 'onHold' | 'resolved' | 'closed';
  priority: 'critical' | 'high' | 'medium' | 'low';
  category: 'bug' | 'feature' | 'support' | 'security' | 'performance';
  assignedTo?: User;
  reporter?: User;
  watchers?: User[];
  labels?: string[];
  comments?: Comment[];
  attachments?: Attachment[];
  relatedIncidents?: RelatedIncident[];
  customFields?: string[];
  metadata?: IncidentMetadata;
  sla?: SlaInfo;
}

export interface IncidentFilters {
  statuses?: ('open' | 'inProgress' | 'onHold' | 'resolved' | 'closed')[];
  priorities?: ('critical' | 'high' | 'medium' | 'low')[];
  categories?: ('bug' | 'feature' | 'support' | 'security' | 'performance')[];
  assigneeId?: string;
  searchQuery?: string;
  dateFrom?: string | null;
  dateTo?: string | null;
  labels?: string[];
}

export interface SortConfig {
  field?: string;
  direction?: string;
}

export interface PaginationState {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface UiState {
  sidebarOpen: boolean;
  activeView?: string;
  sort?: SortConfig;
  pagination?: PaginationState;
}

export interface IncidentAppState {
  incidents?: Incident[];
  selectedIncident?: Incident;
  currentUser?: User;
  isLoading: boolean;
  error?: string;
  filters?: IncidentFilters;
  ui?: UiState;
}