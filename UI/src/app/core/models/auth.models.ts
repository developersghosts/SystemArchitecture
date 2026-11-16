export interface LoginRequest {
  userName: string;
  password: string;
  forceLogin?: boolean;
}

export interface LoginResponse {
  success: boolean;
  message: string;
}

export interface VerifyOtpRequest {
  userName: string;
  password: string;
  otp: string;
}

/** Returned by POST /api/Account/verify-otp. */
export interface AuthResponse {
  id: string;
  userName: string;
  fullName: string;
  email: string;
  dateOfBirth: string | null;
  roles: string[];
  token: string;
  expiresOnUtc: string;
}

export type AuthUser = Omit<AuthResponse, 'token' | 'expiresOnUtc'>;

export interface Session {
  user: AuthUser;
  token: string;
  /** Unix time in ms, taken from the JWT "exp" claim. */
  expiresAt: number;
}

/** Kept in memory only, between the login and verify-otp steps. */
export interface PendingLogin {
  userName: string;
  password: string;
  message: string;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  totalPages: number;
}

export interface UserListItem {
  id: string;
  userName: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string | null;
  dateOfBirth: string | null;
  isActive: boolean;
  createdAtUtc: string;
  lastActiveAtUtc: string | null;
}

export interface RoleListItem {
  id: string;
  name: string;
  title: string;
  description: string | null;
}

export interface UserRole {
  userId: string;
  userName: string;
  fullName: string;
  roleId: string;
  roleName: string;
  roleTitle: string;
}

export interface GroupRole {
  id: string;
  name: string;
  title: string;
}

export interface UserGroupListItem {
  id: string;
  name: string;
  description: string | null;
  memberCount: number;
  roles: GroupRole[];
}

export interface UserGroupDetails {
  id: string;
  name: string;
  description: string | null;
  roles: GroupRole[];
  members: { userId: string; userName: string; fullName: string; email: string }[];
  createdAtUtc: string;
  modifiedAtUtc: string | null;
}

/** Body of POST /api/Account/registerUserFull (roleIds, groupIds) and /register (no roles). */
export interface CreateUserRequest {
  firstName: string;
  lastName: string;
  userName: string;
  email: string;
  password: string;
  dateOfBirth: string | null;
  phoneNumber: string | null;
  roleIds?: string[];
  groupIds?: string[];
}

/** Body of POST /api/Account/editUser. */
export interface EditUserRequest {
  id: string;
  firstName: string;
  lastName: string;
  userName: string;
  email: string;
  dateOfBirth: string | null;
  phoneNumber: string | null;
  isActive: boolean;
}

/** Role names from ServicesA.Domain IdentityRoles. */
export const Roles = {
  Administrator: 'Administrator',
  RegisterUser: 'RegisterUser',
  EditUser: 'EditUser',
  GetAllUsers: 'GetAllUsers',
  GetAllRolesList: 'GetAllRolesList',
  CreateRole: 'CreateRole',
  CreateUserRole: 'CreateUserRole'
} as const;
