/**
 * Phát âm thanh thông báo nhẹ nhàng (0.25s) bằng Web Audio API
 * Tần số hòa âm êm dịu (sine wave 880Hz -> 1760Hz), không phụ thuộc file mp3 bên ngoài
 */
export const playNotificationSound = () => {
  try {
    const AudioContextClass = window.AudioContext || window.webkitAudioContext;
    if (!AudioContextClass) return;

    const ctx = new AudioContextClass();
    if (ctx.state === 'suspended') {
      ctx.resume().catch(() => {});
    }

    const now = ctx.currentTime;

    // Âm chính 1: Nốt A5 (880Hz) mượt mà
    const osc1 = ctx.createOscillator();
    const gain1 = ctx.createGain();
    osc1.type = 'sine';
    osc1.frequency.setValueAtTime(880, now);
    osc1.frequency.exponentialRampToValueAtTime(1320, now + 0.12);

    gain1.gain.setValueAtTime(0.08, now);
    gain1.gain.exponentialRampToValueAtTime(0.0001, now + 0.28);

    osc1.connect(gain1);
    gain1.connect(ctx.destination);

    // Âm phụ 2: Chuông ngọc lanh lảnh nhẹ (1760Hz)
    const osc2 = ctx.createOscillator();
    const gain2 = ctx.createGain();
    osc2.type = 'sine';
    osc2.frequency.setValueAtTime(1760, now + 0.05);

    gain2.gain.setValueAtTime(0.04, now + 0.05);
    gain2.gain.exponentialRampToValueAtTime(0.0001, now + 0.25);

    osc2.connect(gain2);
    gain2.connect(ctx.destination);

    osc1.start(now);
    osc1.stop(now + 0.3);
    osc2.start(now + 0.05);
    osc2.stop(now + 0.3);
  } catch (err) {
    // Trình duyệt có thể chặn âm thanh nếu chưa có tương tác người dùng ban đầu
    console.debug('Web Audio API notification chime bypassed:', err);
  }
};
