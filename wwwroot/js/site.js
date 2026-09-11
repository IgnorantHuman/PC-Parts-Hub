$(document).ready(function () {

    // ==========================================
    // 1. Light and dark theme
    // ==========================================

    const savedTheme =
        localStorage.getItem("pcPartsTheme") || "light";

    applyTheme(savedTheme);

    $("#themeToggle").on("click", function () {

        const newTheme =
            $("body").hasClass("dark-theme")
                ? "light"
                : "dark";

        applyTheme(newTheme);

        localStorage.setItem(
            "pcPartsTheme",
            newTheme
        );
    });

    function applyTheme(theme) {

        const darkTheme =
            theme === "dark";

        $("body").toggleClass(
            "dark-theme",
            darkTheme
        );

        $("#themeIcon").attr(
            "class",
            darkTheme
                ? "bi bi-sun-fill"
                : "bi bi-moon-stars-fill"
        );

        $("#themeToggle").attr(
            "title",
            darkTheme
                ? "Use light theme"
                : "Use dark theme"
        );
    }

    // ==========================================
    // 2. Rotating homepage text
    // ==========================================

    const rotatingText =
        $("#rotatingText");

    if (rotatingText.length) {

        const messages = [
            "best performance",
            "smarter choices",
            "reliable hardware"
        ];

        let currentMessage = 0;

        setInterval(function () {

            currentMessage++;

            if (
                currentMessage >=
                messages.length
            ) {
                currentMessage = 0;
            }

            rotatingText.fadeOut(
                250,
                function () {

                    rotatingText
                        .text(
                            messages[
                            currentMessage
                            ]
                        )
                        .fadeIn(250);
                }
            );

        }, 2500);
    }

    // ==========================================
    // 3. Smooth Browse Components button
    // ==========================================

    $(".scroll-link").on(
        "click",
        function (event) {

            const targetId =
                $(this).attr("href");

            if (
                targetId &&
                targetId.startsWith("#")
            ) {
                const target =
                    $(targetId);

                if (target.length) {

                    event.preventDefault();

                    $("html, body").animate(
                        {
                            scrollTop:
                                target.offset().top -
                                60
                        },
                        700
                    );
                }
            }
        }
    );

    // ==========================================
    // 4. Product filtering and searching
    // ==========================================

    let selectedCategory = "all";

    $(".filter-button").on(
        "click",
        function () {

            $(".filter-button")
                .removeClass("active");

            $(this).addClass("active");

            selectedCategory =
                String(
                    $(this).data("filter")
                ).toLowerCase();

            filterProducts();
        }
    );

    $("#productSearch").on(
        "input",
        function () {

            filterProducts();
        }
    );

    $("#clearSearch").on(
        "click",
        function () {

            $("#productSearch").val("");

            selectedCategory = "all";

            $(".filter-button")
                .removeClass("active");

            $(
                '.filter-button[data-filter="all"]'
            ).addClass("active");

            filterProducts();

            $("#productSearch").focus();
        }
    );

    function filterProducts() {
        const searchText = String(
            $("#productSearch").val() || ""
        ).toLowerCase().trim();

        let visibleProducts = 0;

        $(".product-preview").each(function () {
            const product = $(this);
            const productCategory = String(
                product.data("category") || ""
            ).toLowerCase();
            const productName = String(
                product.data("name") || ""
            ).toLowerCase();

            const categoryMatches =
                selectedCategory === "all" ||
                productCategory === selectedCategory;
            const searchMatches =
                searchText === "" ||
                productName.includes(searchText);
            const showProduct =
                categoryMatches && searchMatches;

            product.toggle(showProduct);
            product.find("h3, .product-brand").each(function () {
                highlightSearchResult(this, searchText);
            });

            if (showProduct) {
                visibleProducts++;
            }
        });

        $("#visibleProductCount").text(visibleProducts);
        $("#noProducts").toggle(visibleProducts === 0);
    }

    function highlightSearchResult(element, searchText) {
        const originalText =
            element.dataset.originalText || element.textContent;

        element.dataset.originalText = originalText;
        element.textContent = originalText;

        if (!searchText) {
            return;
        }

        const matchIndex =
            originalText.toLowerCase().indexOf(searchText);

        if (matchIndex < 0) {
            return;
        }

        const before = document.createTextNode(
            originalText.slice(0, matchIndex)
        );
        const highlight = document.createElement("mark");
        highlight.className = "search-highlight";
        highlight.textContent = originalText.slice(
            matchIndex,
            matchIndex + searchText.length
        );
        const after = document.createTextNode(
            originalText.slice(matchIndex + searchText.length)
        );

        element.replaceChildren(before, highlight, after);
    }

    // Run the filter when the page opens.
    filterProducts();

    // Load a small JSON web service with AJAX and refresh it every 30 seconds.
    function updateActiveProductCount() {
        if (!$("#liveActiveProductCount").length) {
            return;
        }

        $.ajax({
            url: "/api/products/active-count",
            method: "GET",
            dataType: "json",
            cache: false
        })
            .done(function (response) {
                if (response && response.success) {
                    $("#liveActiveProductCount").text(response.count);
                    $("#liveCountStatus").text("Updated");
                }
            })
            .fail(function () {
                $("#liveCountStatus").text("Update unavailable");
            });
    }

    updateActiveProductCount();
    window.setInterval(updateActiveProductCount, 30000);

    // ==========================================
    // 5. Wishlist
    // ==========================================

    let favouriteProducts = [];

    try {
        favouriteProducts =
            JSON.parse(
                localStorage.getItem(
                    "pcPartsFavourites"
                ) || "[]"
            );
    }
    catch {
        favouriteProducts = [];
    }

    // Create one heart button for each card.
    $(".preview-card").each(
        function () {

            const card =
                $(this);

            // Do not create a duplicate heart.
            if (
                card.find(
                    ".favorite-button"
                ).length
            ) {
                return;
            }

            const productName =
                card
                    .find(
                        ".preview-button"
                    )
                    .data("product");

            if (!productName) {
                return;
            }

            const button = $(`
                <button type="button"
                        class="favorite-button"
                        data-product="${productName}"
                        title="Add to wishlist"
                        aria-label="Add to wishlist">

                    <i class="bi bi-heart"></i>

                </button>
            `);

            card.prepend(button);
        }
    );

    updateFavouriteButtons();

    $(document).on(
        "click",
        ".favorite-button",
        function () {

            const button =
                $(this);

            const productName =
                String(
                    button.data("product")
                );

            const position =
                favouriteProducts.indexOf(
                    productName
                );

            if (position === -1) {

                favouriteProducts.push(
                    productName
                );

                showWishlistToast(
                    productName +
                    " was added to your wishlist."
                );
            }
            else {

                favouriteProducts.splice(
                    position,
                    1
                );

                showWishlistToast(
                    productName +
                    " was removed from your wishlist."
                );
            }

            localStorage.setItem(
                "pcPartsFavourites",
                JSON.stringify(
                    favouriteProducts
                )
            );

            updateFavouriteButtons();
        }
    );

    function updateFavouriteButtons() {

        $(".favorite-button").each(
            function () {

                const button =
                    $(this);

                const productName =
                    String(
                        button.data(
                            "product"
                        )
                    );

                const selected =
                    favouriteProducts.includes(
                        productName
                    );

                button.toggleClass(
                    "selected",
                    selected
                );

                button
                    .find("i")
                    .attr(
                        "class",
                        selected
                            ? "bi bi-heart-fill"
                            : "bi bi-heart"
                    );

                button.attr(
                    "title",
                    selected
                        ? "Remove from wishlist"
                        : "Add to wishlist"
                );
            }
        );

        $("#wishlistCount").text(
            favouriteProducts.length
        );
    }

    function showWishlistToast(
        message
    ) {
        $("#wishlistToastMessage")
            .text(message);

        const toastElement =
            document.getElementById(
                "wishlistToast"
            );

        if (
            toastElement &&
            window.bootstrap
        ) {
            const toast =
                bootstrap.Toast
                    .getOrCreateInstance(
                        toastElement,
                        {
                            delay: 2200
                        }
                    );

            toast.show();
        }
    }

    // ==========================================
    // 6. Back to Top and navigation
    // ==========================================

    function updateScrollDesign() {

        const scrollPosition =
            $(window).scrollTop();

        $("#backToTop").toggleClass(
            "show",
            scrollPosition > 350
        );

        $(".pc-navbar").toggleClass(
            "navbar-scrolled",
            scrollPosition > 50
        );
    }

    $(window).on(
        "scroll",
        updateScrollDesign
    );

    updateScrollDesign();

    $("#backToTop").on(
        "click",
        function (event) {

            event.preventDefault();

            $("html, body")
                .stop(true, true)
                .scrollTop(0);
        }
    );

    // ==========================================
    // 7. Scroll entrance animation
    // ==========================================

    if (
        "IntersectionObserver" in
        window
    ) {
        const observer =
            new IntersectionObserver(
                function (entries) {

                    entries.forEach(
                        function (entry) {

                            if (
                                entry
                                    .isIntersecting
                            ) {
                                entry.target
                                    .classList
                                    .add(
                                        "revealed"
                                    );

                                observer
                                    .unobserve(
                                        entry.target
                                    );
                            }
                        }
                    );
                },
                {
                    threshold: 0.12
                }
            );

        document
            .querySelectorAll(
                ".reveal-section"
            )
            .forEach(
                function (section) {

                    observer.observe(
                        section
                    );
                }
            );
    }
    else {
        $(".reveal-section")
            .addClass("revealed");
    }

    // ==========================================
    // 9. Show and hide password
    // ==========================================

    $(document).on(
        "click",
        ".password-toggle",
        function () {

            const button =
                $(this);

            const targetId =
                button.attr(
                    "data-target"
                );

            const passwordInput =
                $("#" + targetId);

            if (
                !passwordInput.length
            ) {
                console.error(
                    "Password field not found:",
                    targetId
                );

                return;
            }

            const currentlyHidden =
                passwordInput.attr(
                    "type"
                ) === "password";

            passwordInput.attr(
                "type",
                currentlyHidden
                    ? "text"
                    : "password"
            );

            button
                .find("i")
                .attr(
                    "class",
                    currentlyHidden
                        ? "bi bi-eye-slash"
                        : "bi bi-eye"
                );

            button.attr(
                "title",
                currentlyHidden
                    ? "Hide password"
                    : "Show password"
            );
        }
    );

    // ==========================================
    // 10. Live password requirements
    // ==========================================

    const passwordInput =
        $("#Input_Password");

    passwordInput.on(
        "input",
        function () {

            checkPasswordRules(
                passwordInput.val()
            );
        }
    );

    function checkPasswordRules(
        password
    ) {
        password =
            String(password || "");

        updatePasswordRule(
            "#lengthRule",
            password.length >= 8
        );

        updatePasswordRule(
            "#upperRule",
            /[A-Z]/.test(password)
        );

        updatePasswordRule(
            "#lowerRule",
            /[a-z]/.test(password)
        );

        updatePasswordRule(
            "#numberRule",
            /[0-9]/.test(password)
        );
    }

    function updatePasswordRule(
        ruleId,
        valid
    ) {
        const rule =
            $(ruleId);

        if (!rule.length) {
            return;
        }

        rule.toggleClass(
            "valid",
            valid
        );

        rule
            .find("i")
            .attr(
                "class",
                valid
                    ? "bi bi-check-circle-fill"
                    : "bi bi-circle"
            );
    }

    // Check an automatically filled password.
    if (passwordInput.length) {
        checkPasswordRules(
            passwordInput.val()
        );
    }

    // ==========================================
    // 11. Login and Register loading effect
    // ==========================================

    $("#account, #registerForm").on(
        "submit",
        function () {

            const form =
                $(this);

            // Do not show loading when
            // validation has failed.
            if (
                typeof form.valid ===
                "function" &&
                !form.valid()
            ) {
                return;
            }

            const submitButton =
                form.find(
                    ".auth-submit-button"
                );

            submitButton
                .find(".button-text")
                .addClass("d-none");

            submitButton
                .find(".button-loading")
                .removeClass("d-none");

            submitButton.prop(
                "disabled",
                true
            );
        }
    );
});
