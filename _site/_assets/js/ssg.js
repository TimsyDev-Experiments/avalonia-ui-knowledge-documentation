(function () {
  "use strict";

  var currentPage = typeof CURRENT_PAGE !== "undefined" ? CURRENT_PAGE : "index.html";
  var origBase = window.location.href;
  var contentBase = origBase;
  var searchInput = document.getElementById("searchInput");
  var searchResults = document.getElementById("searchResults");
  var docContent = document.getElementById("docContent");
  var toc = document.getElementById("toc");
  var headings = [];
  var tocLinks = [];

  /* ===== SPA navigation ===== */
  function navHrefToAbs(href, base) { return new URL(href, base || origBase); }

  function navigateTo(url, base) {
    var absUrl = navHrefToAbs(url, base);
    var absPath = absUrl.pathname;
    if (absPath === currentPage) return;
    var sidebar = document.getElementById("sidebar");
    if (sidebar) { sessionStorage.setItem("sidebarScroll", sidebar.scrollTop); }
    fetch(absPath).then(function (r) {
      if (!r.ok) { window.location.href = absPath; return null; }
      return r.text();
    }).then(function (html) {
      if (!html) return;
      var parser = new DOMParser();
      var doc = parser.parseFromString(html, "text/html");
      var newContent = doc.querySelector(".doc-content");
      if (!newContent || !docContent) { window.location.href = absPath; return; }
      docContent.innerHTML = newContent.innerHTML;
      document.title = doc.title;
      currentPage = absPath;
      contentBase = absUrl.href;
      /* Update nav active state */
      var navLinks = document.querySelectorAll(".nav-item a");
      var activeLink = null;
      for (var i = 0; i < navLinks.length; i++) {
        navLinks[i].closest(".nav-item").classList.remove("active");
        if (navHrefToAbs(navLinks[i].getAttribute("href")).pathname === absPath) {
          navLinks[i].closest(".nav-item").classList.add("active");
          activeLink = navLinks[i];
        }
      }
      if (activeLink) {
        var sb = document.getElementById("sidebar");
        if (sb) {
          setTimeout(function () { activeLink.scrollIntoView({ block: "nearest" }); }, 0);
        }
      }
      /* Restore sidebar scroll */
      var saved = sessionStorage.getItem("sidebarScroll");
      var sb2 = document.getElementById("sidebar");
      if (saved !== null && sb2) { sb2.scrollTop = parseInt(saved, 10); }
      /* Re-init features */
      initTOC();
      initQuizzes();
      if (typeof hljs !== "undefined") { hljs.highlightAll(); }
      window.scrollTo(0, 0);
      history.pushState({ url: absPath }, "", absPath);
    }).catch(function () { window.location.href = absPath; });
  }

  /* Intercept sidebar nav clicks */
  function bindNavClicks() {
    var links = document.querySelectorAll(".nav-item a");
    for (var i = 0; i < links.length; i++) {
      (function (lnk) {
        lnk.addEventListener("click", function (e) {
          e.preventDefault();
          navigateTo(lnk.getAttribute("href"));
        });
      })(links[i]);
    }
  }

  /* Handle back/forward */
  window.addEventListener("popstate", function (e) {
    if (e.state && e.state.url) {
      var popPath = e.state.url;
      var sidebar = document.getElementById("sidebar");
      if (sidebar) { sessionStorage.setItem("sidebarScroll", sidebar.scrollTop); }
      var saved = sessionStorage.getItem("sidebarScroll");
      fetch(popPath).then(function (r) { return r.ok ? r.text() : null; }).then(function (html) {
        if (!html) { window.location.href = popPath; return; }
        var parser = new DOMParser();
        var doc = parser.parseFromString(html, "text/html");
        var newContent = doc.querySelector(".doc-content");
        if (!newContent || !docContent) { window.location.href = popPath; return; }
        docContent.innerHTML = newContent.innerHTML;
        document.title = doc.title;
        currentPage = popPath;
        contentBase = popPath;
        var navLinks = document.querySelectorAll(".nav-item a");
        for (var i = 0; i < navLinks.length; i++) {
          navLinks[i].closest(".nav-item").classList.remove("active");
          if (navHrefToAbs(navLinks[i].getAttribute("href")).pathname === popPath) {
            navLinks[i].closest(".nav-item").classList.add("active");
          }
        }
        var sb2 = document.getElementById("sidebar");
        if (saved !== null && sb2) { sb2.scrollTop = parseInt(saved, 10); }
        initTOC();
        initQuizzes();
        if (typeof hljs !== "undefined") { hljs.highlightAll(); }
        window.scrollTo(0, 0);
      });
    }
  });

  /* ===== Search ===== */
  if (searchInput && searchResults && typeof SEARCH_INDEX !== "undefined") {
    var searchTimeout = null;
    searchInput.addEventListener("input", function () {
      clearTimeout(searchTimeout);
      searchTimeout = setTimeout(doSearch, 150);
    });
    function doSearch() {
      var q = searchInput.value.trim().toLowerCase();
      if (q.length < 2) { searchResults.style.display = "none"; return; }
      var terms = q.split(/\s+/);
      var results = [];
      for (var i = 0; i < SEARCH_INDEX.length; i++) {
        var entry = SEARCH_INDEX[i];
        var haystack = (entry.title + " " + entry.text + " " + entry.tier + " " + (entry.headings || []).join(" ")).toLowerCase();
        var match = true;
        for (var t = 0; t < terms.length; t++) { if (haystack.indexOf(terms[t]) === -1) { match = false; break; } }
        if (match) results.push(entry);
      }
      results.sort(function (a, b) { return a.title.localeCompare(b.title); });
      if (results.length > 50) results = results.slice(0, 50);
      if (results.length === 0) {
        searchResults.innerHTML = "<a style=\"color:var(--text-dim);cursor:default\">No results found</a>";
      } else {
        var html = "";
        for (var r = 0; r < results.length; r++) {
          html += "<a href=\"" + results[r].path + "\">" + escapeHtml(results[r].title) + "<span class=\"search-result-tier\">" + escapeHtml(results[r].tier) + "</span></a>";
        }
        searchResults.innerHTML = html;
      }
      searchResults.style.display = "block";
    }
  }

  bindNavClicks();

  /* ===== Global link interception ===== */
  document.addEventListener("click", function (e) {
    var target = e.target.closest ? e.target.closest("a") : null;
    if (!target) return;
    var href = target.getAttribute("href");
    if (!href) return;

    /* TOC anchor links — smooth scroll */
    if (href.startsWith("#")) {
      var id = href.slice(1);
      var el = document.getElementById(id);
      if (el) { e.preventDefault(); el.scrollIntoView({ behavior: "smooth" }); }
      return;
    }

    /* Search result links */
    if (target.closest(".search-results")) {
      e.preventDefault();
      if (searchResults) searchResults.style.display = "none";
      if (searchInput) searchInput.value = "";
      navigateTo(href);
      return;
    }

    /* Sidebar nav links — already handled by bindNavClicks */
    if (target.closest(".nav-item")) return;

    /* Same-origin .html links — route through SPA */
    if (href.endsWith(".html") && !href.startsWith("http") && !href.startsWith("//")) {
      e.preventDefault();
      navigateTo(href, contentBase);
    }
  });

  /* ===== TOC generation ===== */
  function initTOC() {
    headings = [];
    tocLinks = [];
    if (!docContent || !toc) return;
    var headingEls = docContent.querySelectorAll("h2, h3");
    toc.classList.toggle("visible", headingEls.length > 0);
    if (headingEls.length === 0) { toc.innerHTML = ""; return; }
    var tocHtml = "<h3>On this page</h3>";
    for (var i = 0; i < headingEls.length; i++) {
      var el = headingEls[i];
      var id = el.id || el.textContent.trim().toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "");
      el.id = id;
      var tag = el.tagName.toLowerCase();
      tocHtml += "<a href=\"#" + id + "\" class=\"toc-" + tag + "\">" + escapeHtml(el.textContent) + "</a>";
      headings.push({ id: id, el: el });
    }
    toc.innerHTML = tocHtml;
  }

  /* Scrollspy for TOC */
  window.addEventListener("scroll", function () {
    if (headings.length === 0) return;
    var scrollY = window.scrollY + 100;
    var active = null;
    for (var i = headings.length - 1; i >= 0; i--) {
      if (headings[i].el.offsetTop <= scrollY) { active = headings[i].id; break; }
    }
    var links = toc ? toc.querySelectorAll("a") : [];
    for (var i = 0; i < links.length; i++) { links[i].classList.toggle("toc-active", links[i].getAttribute("href") === "#" + active); }
  });

  /* ===== Quiz widget ===== */
  function initQuizzes() {
    var containers = docContent ? docContent.querySelectorAll(".quiz-container") : [];
    for (var qi = 0; qi < containers.length; qi++) { initQuiz(containers[qi]); }
  }

  function initQuiz(container) {
    var options = container.querySelectorAll(".quiz-option");
    var feedback = container.querySelector(".quiz-feedback");
    var answered = false;
    var total = options.length;
    var correctCount = 0;

    for (var i = 0; i < options.length; i++) {
      (function (opt) {
        opt.addEventListener("click", function () {
          if (answered) return;
          var isCorrect = opt.classList.contains("correct");
          if (isCorrect) { opt.classList.add("correct"); correctCount++; }
          else { opt.classList.add("incorrect"); }
          answered = true;
          container.classList.add("quiz-completed");
          if (feedback) {
            feedback.style.display = "block";
            feedback.textContent = correctCount === total ? "All correct!" : correctCount + " of " + total + " correct.";
            feedback.classList.add("show");
          }
          for (var j = 0; j < options.length; j++) {
            var exp = options[j].querySelector(".option-explanation");
            if (exp) { exp.style.maxHeight = exp.scrollHeight + "px"; exp.style.opacity = "1"; }
          }
        });
      })(options[i]);
    }
  }

  /* ===== Highlight.js ===== */
  if (typeof hljs !== "undefined") { hljs.highlightAll(); }

  /* ===== Helpers ===== */
  function escapeHtml(s) {
    if (!s) return "";
    return s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
  }

  /* Init on load */
  initTOC();
  initQuizzes();

})();
