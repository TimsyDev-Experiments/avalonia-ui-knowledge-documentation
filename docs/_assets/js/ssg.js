(function () {
  "use strict";

  /* ===== Search ===== */
  var searchInput = document.getElementById("searchInput");
  var searchResults = document.getElementById("searchResults");
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

  /* ===== Nav tree active state on scroll ===== */
  var navLinks = document.querySelectorAll(".nav-item a");
  var headings = [];
  var tocLinks = [];
  if (typeof CURRENT_PAGE !== "undefined") {
    /* Highlight current nav link */
    for (var i = 0; i < navLinks.length; i++) {
      var href = navLinks[i].getAttribute("href");
      if (href === CURRENT_PAGE) { navLinks[i].closest(".nav-item").classList.add("active"); break; }
    }
  }

  /* ===== TOC generation ===== */
  var docContent = document.getElementById("docContent");
  var toc = document.getElementById("toc");
  if (docContent && toc) {
    var headingEls = docContent.querySelectorAll("h2, h3");
    if (headingEls.length > 0) {
      toc.classList.add("visible");
      var tocHtml = "<h3>On this page</h3>";
      for (var i = 0; i < headingEls.length; i++) {
        var el = headingEls[i];
        var id = el.id || el.textContent.trim().toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "");
        el.id = id;
        var tag = el.tagName.toLowerCase();
        tocHtml += "<a href=\"#" + id + "\" class=\"toc-" + tag + "\">" + escapeHtml(el.textContent) + "</a>";
        headings.push({ id: id, el: el });
        tocLinks.push(toc.lastElementChild);
      }
      toc.innerHTML = tocHtml;
      /* Scrollspy */
      window.addEventListener("scroll", function () {
        var scrollY = window.scrollY + 100;
        var active = null;
        for (var i = headings.length - 1; i >= 0; i--) {
          if (headings[i].el.offsetTop <= scrollY) { active = headings[i].id; break; }
        }
        var links = toc.querySelectorAll("a");
        for (var i = 0; i < links.length; i++) { links[i].classList.toggle("toc-active", links[i].getAttribute("href") === "#" + active); }
      });
    }
  }

  /* ===== Quiz widget ===== */
  var quizContainers = document.querySelectorAll(".quiz-container");
  for (var qi = 0; qi < quizContainers.length; qi++) {
    initQuiz(quizContainers[qi]);
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
          /* Reveal all explanations */
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

})();
