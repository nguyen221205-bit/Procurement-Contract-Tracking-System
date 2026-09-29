import React, { createContext, useContext, useState, useEffect } from 'react';
import { authApi } from '../api/authApi';
import toast from 'react-hot-toast';

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(() => {
    const savedUser = localStorage.getItem('user');
    return savedUser ? JSON.parse(savedUser) : null;
  });
  const [token, setToken] = useState(() => localStorage.getItem('token') || null);
  const [loading, setLoading] = useState(false);

  const login = async (email, password) => {
    setLoading(true);
    try {
      const response = await authApi.login(email, password);
      if (response && response.success && response.data) {
        const { token: jwtToken, refreshToken, user: userData } = response.data;
        
        localStorage.setItem('token', jwtToken);
        localStorage.setItem('refreshToken', refreshToken);
        localStorage.setItem('user', JSON.stringify(userData));
        
        setToken(jwtToken);
        setUser(userData);
        toast.success(`Đăng nhập thành công! Xin chào, ${userData.fullName}`);
        return { success: true, user: userData };
      } else {
        const errorMsg = response?.message || 'Đăng nhập không thành công';
        toast.error(errorMsg);
        return { success: false, message: errorMsg };
      }
    } catch (error) {
      toast.error(error.message || 'Lỗi kết nối tới máy chủ');
      return { success: false, message: error.message };
    } finally {
      setLoading(false);
    }
  };

  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    setToken(null);
    setUser(null);
    toast.success('Đã đăng xuất khỏi hệ thống');
  };

  const hasRole = (allowedRoles) => {
    if (!user || !user.roles) return false;
    if (typeof allowedRoles === 'string') {
      return user.roles.includes(allowedRoles);
    }
    if (Array.isArray(allowedRoles)) {
      return allowedRoles.some((role) => user.roles.includes(role));
    }
    return false;
  };

  const primaryRole = user?.roles?.[0] || null;

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isAuthenticated: !!token && !!user,
        role: primaryRole,
        roles: user?.roles || [],
        loading,
        login,
        logout,
        hasRole,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
