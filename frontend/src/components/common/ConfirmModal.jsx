import React from 'react';
import { AlertTriangle, Info, X } from 'lucide-react';

export const ConfirmModal = ({
  isOpen,
  onClose,
  onConfirm,
  title = 'Xác nhận thao tác',
  message = 'Bạn có chắc chắn muốn thực hiện hành động này?',
  confirmText = 'Xác nhận',
  cancelText = 'Hủy bỏ',
  confirmVariant = 'primary', // 'primary', 'warning', 'danger'
  isLoading = false,
}) => {
  if (!isOpen) return null;

  const variantStyles = {
    primary: {
      button: 'bg-sky-600 hover:bg-sky-700 text-white focus:ring-sky-500',
      iconBg: 'bg-sky-50 text-sky-600 border-sky-100',
      icon: <Info className="w-5 h-5 text-sky-600" />,
    },
    warning: {
      button: 'bg-amber-600 hover:bg-amber-700 text-white focus:ring-amber-500',
      iconBg: 'bg-amber-50 text-amber-600 border-amber-100',
      icon: <AlertTriangle className="w-5 h-5 text-amber-600" />,
    },
    danger: {
      button: 'bg-rose-600 hover:bg-rose-700 text-white focus:ring-rose-500',
      iconBg: 'bg-rose-50 text-rose-600 border-rose-100',
      icon: <AlertTriangle className="w-5 h-5 text-rose-600" />,
    },
  };

  const style = variantStyles[confirmVariant] || variantStyles.primary;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/50 backdrop-blur-xs flex items-center justify-center p-4 animate-in fade-in duration-150">
      <div
        className="bg-white rounded-2xl max-w-md w-full p-6 shadow-xl border border-slate-100 relative space-y-4"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Close Button */}
        <button
          onClick={onClose}
          disabled={isLoading}
          className="absolute top-4 right-4 text-slate-400 hover:text-slate-600 p-1 rounded-lg hover:bg-slate-100 transition"
        >
          <X className="w-4 h-4" />
        </button>

        {/* Header Icon + Title */}
        <div className="flex items-start space-x-3.5">
          <div className={`p-2.5 rounded-xl border flex-shrink-0 ${style.iconBg}`}>
            {style.icon}
          </div>
          <div className="space-y-1 pr-6">
            <h3 className="text-base font-bold text-slate-900">{title}</h3>
            <p className="text-xs text-slate-500 leading-relaxed">{message}</p>
          </div>
        </div>

        {/* Actions */}
        <div className="pt-2 flex items-center justify-end space-x-2.5">
          <button
            type="button"
            onClick={onClose}
            disabled={isLoading}
            className="px-4 py-2 text-xs font-semibold text-slate-600 hover:bg-slate-100 rounded-xl transition border border-slate-200 disabled:opacity-50"
          >
            {cancelText}
          </button>
          <button
            type="button"
            onClick={onConfirm}
            disabled={isLoading}
            className={`px-4 py-2 text-xs font-semibold rounded-xl transition shadow-xs focus:ring-2 focus:ring-offset-1 disabled:opacity-50 flex items-center space-x-1.5 ${style.button}`}
          >
            {isLoading && (
              <svg className="animate-spin -ml-1 mr-1.5 h-3.5 w-3.5 text-white" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
            )}
            <span>{confirmText}</span>
          </button>
        </div>
      </div>
    </div>
  );
};

export default ConfirmModal;
