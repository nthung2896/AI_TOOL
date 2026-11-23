(() => {
  window.__rentalMessengerSetup?.cleanup?.();
  const box = document.createElement('div');
  box.id = 'rental-messenger-setup';
  Object.assign(box.style, { position: 'fixed', top: '12px', left: '50%', transform: 'translateX(-50%)', zIndex: '2147483647', width: 'min(640px,90vw)', background: '#172554', color: 'white', padding: '16px 20px', borderRadius: '12px', font: '15px/1.6 Arial', boxShadow: '0 8px 30px #0004' });
  box.setAttribute('role', 'status');
  document.body.append(box);
  const setup = window.__rentalMessengerSetup = { result: null, cleanup: () => { document.removeEventListener('click', click, true); box.remove(); } };
  let incoming = null;
  function label(text) { box.textContent = text; }
  label('Thiết lập Messenger · Bước 1/2: bấm vào nội dung một tin KHÁCH gửi. Chọn tin đang thấy trong hội thoại, không chọn tên hay ảnh đại diện.');
  function rootFor(target) {
    for (let node = target; node && node !== document.body; node = node.parentElement) {
      for (const attr of ['data-message-id', 'data-mid', 'data-messageid', 'data-id', 'id']) {
        const value = node.getAttribute(attr);
        if (!value || value.length > 250) continue;
        if (['data-id', 'id'].includes(attr) && !/^mid[.$:_-]/i.test(value)) continue;
        return { node, attr, value, base: ['id', 'data-id'].includes(attr) ? `[${attr}^=${JSON.stringify(value.slice(0,4))}]` : `[${attr}]` };
      }
    }
    return null;
  }
  function tokens(node) {
    const list = [];
    for (const attr of ['data-testid', 'data-message-direction', 'data-is-outgoing', 'data-sender-type', 'data-owner', 'dir']) {
      const value = node.getAttribute(attr);
      if (value && value.length < 120) list.push(`[${attr}=${JSON.stringify(value)}]`);
    }
    const classes = [...node.classList].filter(c => c.length < 80);
    classes.forEach(c => list.push('.' + CSS.escape(c)));
    for (let a = 0; a < Math.min(classes.length, 10); a++) for (let b = a + 1; b < Math.min(classes.length, 10); b++) list.push('.' + CSS.escape(classes[a]) + '.' + CSS.escape(classes[b]));
    return list;
  }
  function selectorFor(sample, opposite) {
    const candidates = [];
    for (let node = sample.node, depth = 0; node && depth < 6; node = node.parentElement, depth++) {
      for (const token of tokens(node)) {
        const selector = depth === 0 ? sample.base + token : token + ' ' + sample.base;
        try {
          if (!sample.node.matches(selector) || opposite.node.matches(selector)) continue;
          const count = document.querySelectorAll(selector).length;
          candidates.push({ selector, count, priority: token.startsWith('[data-') ? 10 : count > 1 ? 5 : 0 });
        } catch { }
      }
    }
    return candidates.sort((a,b) => b.priority-a.priority || b.count-a.count || a.selector.length-b.selector.length)[0]?.selector;
  }
  function fail(text) {
    setup.result = { incomingMessages: '', outgoingMessages: '', messageIdAttribute: '', incomingId: '', outgoingId: '', error: text };
    document.removeEventListener('click', click, true); label(text);
  }
  function click(event) {
    if (box.contains(event.target)) return;
    event.preventDefault(); event.stopImmediatePropagation();
    const sample = rootFor(event.target);
    if (!sample) { label('Chưa tìm thấy ID tin ổn định ở vị trí này. Thử bấm trực tiếp vào bong bóng tin nhắn. Nếu vẫn không được, hãy hủy thiết lập; ứng dụng sẽ không đoán ID từ nội dung.'); return; }
    if (!incoming) { incoming = sample; label('Bước 2/2: bấm vào nội dung một tin do BẠN gửi trong cùng hội thoại. Chỉ chọn tin có sẵn, không cần gửi tin mới.'); return; }
    if (incoming.node === sample.node || incoming.attr !== sample.attr || incoming.value === sample.value) { label('Hãy chọn một tin KHÁC do bạn gửi, cùng hội thoại.'); return; }
    const incomingSelector = selectorFor(incoming, sample), outgoingSelector = selectorFor(sample, incoming);
    if (!incomingSelector || !outgoingSelector) return fail('Không tìm được đặc điểm DOM phân biệt hai chiều tin. Không lưu cấu hình; cần kiểm tra cấu trúc Messenger trên máy này.');
    if ([...document.querySelectorAll(incomingSelector)].some(node => node.matches(outgoingSelector))) return fail('Hai bộ nhận diện bị trùng. Không lưu cấu hình.');
    setup.result = { incomingMessages: incomingSelector, outgoingMessages: outgoingSelector, messageIdAttribute: incoming.attr, incomingId: incoming.value, outgoingId: sample.value, error: '' };
    document.removeEventListener('click', click, true);
    label('Đã chọn đủ hai mẫu. Ứng dụng đang tải lại hội thoại để kiểm tra nhận diện trước khi lưu.');
  }
  document.addEventListener('click', click, true);
})();
