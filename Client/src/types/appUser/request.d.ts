
export interface AppUserEditViewModelType  {
  familyName?: string;
  name?: string;
  givenName?: string;
  gender: number;
  cCCD?: string;
  avatar?: string;
  phoneNumber?: string;
  email?: string;
  diaChi?: string;
  ngaySinh?: Date;
}

export interface ChangePasswordViewModelType  {
  oldPassword?: string;
  newPassword?: string;
  confirmPassword?: string;
}

export interface ForgotPasswordViewModelType  {
  userName?: string;
  email?: string;
  url?: string;
}

export interface LoginViewModelType  {
  userName?: string;
  password?: string;
}

export interface LoginSSOViewModelType  {
  code?: string;
}

export interface ProfileUserEditRequestType  {
  id?: string;
  name?: string;
  gender: number;
  ngaySinh?: Date;
  phoneNumber?: string;
  diaChi?: string;
}

export interface RegisterViewModelType  {
  email?: string;
  name?: string;
  gender?: number;
  userName?: string;
  password?: string;
  confirmPassword?: string;
  diaChi?: string;
  phoneNumber?: string;
}

export interface ResetPasswordViewModelType  {
  userName?: string;
  password?: string;
  confirmPassword?: string;
  token?: string;
}

