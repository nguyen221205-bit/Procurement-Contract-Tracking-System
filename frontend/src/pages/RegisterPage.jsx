import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ShieldCheck, Building2, Briefcase } from 'lucide-react';
import ContractorRegisterForm from '../components/auth/ContractorRegisterForm';
import ProcuringEntityRegisterForm from '../components/auth/ProcuringEntityRegisterForm';

const REGISTER_ROLE_TABS = [
  {
    key: 'contractor',
    label: 'Nhà thầu',
    icon: Building2,
    activeClass: 'bg-white text-slate-900 shadow-sm',
  },
  {
    key: 'procuring_entity',
    label: 'Bên mời thầu',
    icon: Briefcase,
    activeClass: 'bg-white text-sky-700 shadow-sm',
  },
];

const REGISTER_FORM_BY_ROLE = {
  contractor: ContractorRegisterForm,
  procuring_entity: ProcuringEntityRegisterForm,
};

export const RegisterPage = () => {
  const navigate = useNavigate();
  const [roleType, setRoleType] = useState('contractor');

  const ActiveFormComponent = REGISTER_FORM_BY_ROLE[roleType] || ContractorRegisterForm;

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col justify-center items-center py-10 px-4 sm:px-6">
      <div className="max-w-xl w-full space-y-6">
        {/* Tiêu đề ứng dụng */}
        <div className="text-center space-y-2">
          <div className="inline-flex items-center justify-center w-12 h-12 rounded-xl bg-slate-900 text-sky-400 shadow-sm">
            <ShieldCheck className="w-6 h-6" />
          </div>
          <h1 className="text-base font-bold text-slate-700 tracking-tight">
            Procurement & Contract Tracking System
          </h1>
        </div>

        {/* Khung nội dung đăng ký */}
        <div className="bg-white p-7 sm:p-9 rounded-2xl border border-slate-200 shadow-sm space-y-6">
          <div>
            <h2 className="text-2xl font-bold text-slate-900">Đăng ký tài khoản</h2>
            <p className="text-xs text-slate-500 mt-1">
              Chọn loại tài khoản phù hợp với đơn vị của bạn
            </p>
          </div>

          {/* Tab chọn loại tài khoản */}
          <div className="grid grid-cols-2 p-1 bg-slate-100 rounded-xl gap-1">
            {REGISTER_ROLE_TABS.map((tab) => {
              const IconComponent = tab.icon;
              const isActive = roleType === tab.key;
              return (
                <button
                  key={tab.key}
                  type="button"
                  onClick={() => setRoleType(tab.key)}
                  className={`py-2 px-3 text-xs font-semibold rounded-lg transition flex items-center justify-center space-x-2 ${
                    isActive ? tab.activeClass : 'text-slate-500 hover:text-slate-800'
                  }`}
                >
                  <IconComponent className="w-4 h-4" />
                  <span>{tab.label}</span>
                </button>
              );
            })}
          </div>

          {/* Hiển thị Form tương ứng qua bảng ánh xạ O(1) */}
          <ActiveFormComponent onSuccess={() => navigate('/login')} />
        </div>
      </div>
    </div>
  );
};

export default RegisterPage;
