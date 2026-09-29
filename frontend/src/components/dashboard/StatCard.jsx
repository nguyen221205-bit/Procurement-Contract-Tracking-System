import React from 'react';

export const StatCard = ({
  title,
  value,
  subtext,
  icon: Icon,
  badgeText,
  color = 'sky',
}) => {
  const colorStyles = {
    sky: 'bg-sky-50 text-sky-700 border-sky-100',
    emerald: 'bg-emerald-50 text-emerald-700 border-emerald-100',
    indigo: 'bg-indigo-50 text-indigo-700 border-indigo-100',
    amber: 'bg-amber-50 text-amber-700 border-amber-100',
  };

  const iconBgStyles = {
    sky: 'bg-sky-100 text-sky-700',
    emerald: 'bg-emerald-100 text-emerald-700',
    indigo: 'bg-indigo-100 text-indigo-700',
    amber: 'bg-amber-100 text-amber-700',
  };

  return (
    <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-xs flex flex-col justify-between space-y-3">
      <div className="flex items-center justify-between">
        <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider">
          {title}
        </span>
        {Icon && (
          <div className={`w-8 h-8 rounded-lg flex items-center justify-center ${iconBgStyles[color] || iconBgStyles.sky}`}>
            <Icon className="w-4 h-4" />
          </div>
        )}
      </div>

      <div>
        <h3 className="text-2xl font-extrabold text-slate-900 tracking-tight">
          {value}
        </h3>
        <div className="flex items-center justify-between mt-1 text-xs">
          {subtext && <span className="text-slate-500">{subtext}</span>}
          {badgeText && (
            <span className={`px-1.5 py-0.5 rounded font-medium border text-[11px] ${colorStyles[color] || colorStyles.sky}`}>
              {badgeText}
            </span>
          )}
        </div>
      </div>
    </div>
  );
};

export default StatCard;
