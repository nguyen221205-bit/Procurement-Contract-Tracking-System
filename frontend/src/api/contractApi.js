import axiosClient from './axiosClient';

export const contractApi = {
  // Lấy danh sách hợp đồng (hỗ trợ phân trang, tìm kiếm, lọc trạng thái)
  getContracts: async (params = {}) => {
    return await axiosClient.get('/contracts', { params });
  },

  // Xem chi tiết hợp đồng kèm mốc thanh toán
  getContractById: async (id) => {
    return await axiosClient.get(`/contracts/${id}`);
  },

  // Tra cứu hợp đồng theo nhà thầu
  getContractsByContractor: async (contractorId) => {
    return await axiosClient.get(`/contracts/contractor/${contractorId}`);
  },

  // Lấy dữ liệu trúng thầu bàn giao sang hợp đồng (Pre-fill)
  getAwardedBidForContract: async (packageId) => {
    return await axiosClient.get(`/contracts/awarded-bid/${packageId}`);
  },

  // Tạo mới hợp đồng kinh tế từ kết quả trúng thầu
  createContract: async (data) => {
    return await axiosClient.post('/contracts', data);
  },

  // Cập nhật hợp đồng (khi ở trạng thái Draft)
  updateContract: async (id, data) => {
    return await axiosClient.put(`/contracts/${id}`, data);
  },

  // Chuyển trạng thái hợp đồng (Draft -> Active -> Completed / Terminated)
  changeStatus: async (id, newStatus, note = null, terminationReasonCategory = null, decisionNumber = null) => {
    const payload = typeof newStatus === 'object'
      ? newStatus
      : { newStatus, note, terminationReasonCategory, decisionNumber };
    return await axiosClient.put(`/contracts/${id}/status`, payload);
  },

  // Tải lên file PDF hợp đồng scan có chữ ký
  uploadScannedFile: async (id, file) => {
    const formData = new FormData();
    formData.append('file', file);
    return await axiosClient.post(`/contracts/${id}/scanned-file`, formData);
  },

  // Quản lý mốc thanh toán (Milestones)
  addMilestone: async (contractId, data) => {
    return await axiosClient.post(`/contracts/${contractId}/milestones`, data);
  },

  updateMilestone: async (contractId, milestoneId, data) => {
    return await axiosClient.put(`/contracts/${contractId}/milestones/${milestoneId}`, data);
  },

  deleteMilestone: async (contractId, milestoneId) => {
    return await axiosClient.delete(`/contracts/${contractId}/milestones/${milestoneId}`);
  },

  // Xác nhận kỹ thuật mốc thanh toán nghiệm thu (Maker - Field/Technical Officer)
  technicalReviewMilestone: async (milestoneId, isPassed = true, note = '') => {
    return await axiosClient.put(`/contracts/milestones/${milestoneId}/technical-review`, {
      isPassed,
      note,
    });
  },

  // Phê duyệt biên bản nghiệm thu mốc thanh toán (Checker - Authorized Signer)
  // F02: Gắn chặt AcceptanceId, ExpectedRevision và ExpectedVersion, loại bỏ default version 1
  approveMilestoneAcceptance: async (milestoneId, dataOrApproved, note = '', expectedVersion, acceptanceId, expectedRevision) => {
    let payload = {};
    if (typeof dataOrApproved === 'object' && dataOrApproved !== null) {
      payload = dataOrApproved;
    } else {
      payload = {
        isApproved: Boolean(dataOrApproved),
        note,
        expectedVersion,
        acceptanceId,
        expectedRevision,
      };
    }
    return await axiosClient.put(`/contracts/milestones/${milestoneId}/acceptance`, payload);
  },

  // T04: Tải tệp tài liệu scan snapshot nghiệm thu kỹ thuật qua axiosClient có xác thực Bearer token và blob response
  downloadAcceptanceSnapshotDocument: async (milestoneId, acceptanceId) => {
    return await axiosClient.get(`/contracts/milestones/${milestoneId}/acceptances/${acceptanceId}/document/download`, {
      responseType: 'blob',
    });
  },

  // F04: Tải tệp tài liệu scan hợp đồng tại thời điểm snapshot nghiệm thu kỹ thuật
  downloadAcceptanceSnapshotDocumentUrl: (milestoneId, acceptanceId) => {
    return `/api/contracts/milestones/${milestoneId}/acceptances/${acceptanceId}/document/download`;
  },

  // Báo cáo tiến độ (Nhà thầu)
  addProgressUpdate: async (contractId, data) => {
    return await axiosClient.post(`/contracts/${contractId}/progress`, data);
  },

  addMilestoneProgressUpdate: async (milestoneId, data) => {
    return await axiosClient.post(`/contracts/milestones/${milestoneId}/progress`, data);
  },

  // ==========================================
  // NHÓM B: BẢO LÃNH HỢP ĐỒNG (CONTRACT GUARANTEES)
  // ==========================================
  getGuarantees: async (contractId) => {
    return await axiosClient.get(`/contracts/${contractId}/guarantees`);
  },

  addGuarantee: async (contractId, data) => {
    return await axiosClient.post(`/contracts/${contractId}/guarantees`, data);
  },

  updateGuaranteeStatus: async (contractId, guaranteeId, status, notes = '') => {
    return await axiosClient.patch(`/contracts/${contractId}/guarantees/${guaranteeId}/status`, {
      status,
      notes,
    });
  },

  uploadGuaranteeDocument: async (contractId, guaranteeId, file) => {
    const formData = new FormData();
    formData.append('file', file);
    return await axiosClient.post(`/contracts/${contractId}/guarantees/${guaranteeId}/document`, formData);
  },

  // ==========================================
  // NHÓM B: PHỤ LỤC HỢP ĐỒNG (CONTRACT ADDENDA)
  // ==========================================
  getAddenda: async (contractId) => {
    return await axiosClient.get(`/contracts/${contractId}/addenda`);
  },

  createAddendum: async (contractId, data) => {
    return await axiosClient.post(`/contracts/${contractId}/addenda`, data);
  },

  approveAddendum: async (contractId, addendumId, isApproved = true, notes = '') => {
    return await axiosClient.patch(`/contracts/${contractId}/addenda/${addendumId}/approve`, {
      isApproved,
      notes,
    });
  },

  uploadAddendumDocument: async (contractId, addendumId, file) => {
    const formData = new FormData();
    formData.append('file', file);
    return await axiosClient.post(`/contracts/${contractId}/addenda/${addendumId}/document`, formData);
  },

  // ==========================================
  // NHÓM B: ĐÁNH GIÁ UY TÍN NHÀ THẦU (CONTRACTOR EVALUATION & CREDIT)
  // ==========================================
  evaluateContractor: async (contractId, data) => {
    return await axiosClient.post(`/contracts/${contractId}/evaluation`, data);
  },

  getContractEvaluation: async (contractId) => {
    return await axiosClient.get(`/contracts/${contractId}/evaluation`);
  },

  getContractorCreditProfile: async (contractorId) => {
    return await axiosClient.get(`/contracts/contractor/${contractorId}/credit-profile`);
  },
};

export default contractApi;
