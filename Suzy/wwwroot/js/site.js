// Theme toggling logic (works on all pages)
document.addEventListener("DOMContentLoaded", () => {
    const body = document.body;
    const toggleBtn = document.getElementById("themeToggle");

    // Apply saved theme from localStorage
    if (localStorage.getItem("theme") === "dark") {
        body.classList.add("theme-dark");
    }

    // Toggle button click
    toggleBtn?.addEventListener("click", () => {
        body.classList.toggle("theme-dark");
        const isDark = body.classList.contains("theme-dark");
        localStorage.setItem("theme", isDark ? "dark" : "light");
    });
});



// These functions are made available globally for the navbar
function copyInviteLink() {
    // This will be called from the navbar, so we need to check if it's available
    if (window.sessionData && window.sessionData.inviteCode) {
        const inviteText = `Join my study session "${window.sessionData.title}" with code: ${window.sessionData.inviteCode}`;
        navigator.clipboard.writeText(inviteText).then(() => {
            alert('Invite link copied to clipboard!');
        }).catch(() => {
            prompt('Copy this invite text:', inviteText);
        });
    } else if (window.sessionData && window.sessionData.isPublic) {
        const shareUrl = `${window.location.origin}/Sessions/SessionManager`;
        navigator.clipboard.writeText(`Join my study session "${window.sessionData.title}" at: ${shareUrl}`).then(() => {
            alert('Session link copied to clipboard!');
        }).catch(() => {
            prompt('Copy this link:', shareUrl);
        });
    } else {
        alert('This session cannot be shared.');
    }
}

function leaveSession() {
    if (!window.sessionId) {
        alert('No active session found.');
        return;
    }

    if (confirm('Are you sure you want to leave this study session?')) {
        fetch(`/api/StudySession/LeaveSession/${window.sessionId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            }
        }).then(response => {
            if (response.ok) {
                return response.json();
            } else {
                throw new Error('Failed to leave session');
            }
        }).then(result => {
            if (result.success) {
                // Clean up break mode styling
                document.body.classList.remove('break-mode');

                // Hide navbar session elements
                const sessionTitleNavbar = document.getElementById('sessionTitleNavbar');
                const sessionActions = document.getElementById('sessionActions');

                if (sessionTitleNavbar) {
                    sessionTitleNavbar.classList.add('d-none');
                }
                if (sessionActions) {
                    sessionActions.classList.add('d-none');
                }

                // Redirect to session manager
                window.location.href = '/Sessions/SessionManager';
            } else {
                alert(result.message || 'Failed to leave session');
            }
        }).catch(error => {
            console.error('Error leaving session:', error);
            alert('Error leaving session. Please try again.');
        });
    }
}

function deleteSession() {
    if (!window.sessionId) {
        alert('No active session found.');
        return;
    }

    if (confirm('Are you sure you want to delete this study session? This action cannot be undone and will remove all participants from the session.')) {
        fetch(`/api/StudySession/DeleteSession/${window.sessionId}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
            }
        }).then(response => {
            if (response.ok) {
                return response.json();
            } else {
                throw new Error('Failed to delete session');
            }
        }).then(result => {
            if (result.success) {
                alert('Session deleted successfully');

                // Hide navbar session elements
                const sessionTitleNavbar = document.getElementById('sessionTitleNavbar');
                const sessionActions = document.getElementById('sessionActions');

                if (sessionTitleNavbar) {
                    sessionTitleNavbar.classList.add('d-none');
                }
                if (sessionActions) {
                    sessionActions.classList.add('d-none');
                }

                // Redirect to session manager
                window.location.href = '/Sessions/SessionManager';
            } else {
                alert(result.message || 'Failed to delete session');
            }
        }).catch(error => {
            console.error('Error deleting session:', error);
            alert('Error deleting session. Please try again.');
        });
    }
}

// Navbar responsive adjustment for sidebar
function adjustNavbarForSidebar() {
    const sidebar = document.getElementById('sidebar');
    const navbarContainer = document.getElementById('navbarContainer');
    const footerContainer = document.getElementById('footerContainer');

    if (sidebar && navbarContainer && footerContainer) {
        const updateContainers = () => {
            const isCollapsed = sidebar.classList.contains('collapsed');
            if (isCollapsed) {
                navbarContainer.style.marginLeft = '80px';
                navbarContainer.style.width = 'calc(100% - 80px)';
                footerContainer.style.marginLeft = '80px';
                footerContainer.style.width = 'calc(100% - 80px)';
            } else {
                navbarContainer.style.marginLeft = '16.666667%';
                navbarContainer.style.width = '83.333333%';
                footerContainer.style.marginLeft = '16.666667%';
                footerContainer.style.width = '83.333333%';
            }
        };

        // Initial adjustment (already set in CSS, but ensure it's applied)
        updateContainers();

        // Listen for sidebar toggle
        const sidebarToggle = document.getElementById('sidebarToggle');
        if (sidebarToggle) {
            sidebarToggle.addEventListener('click', () => {
                setTimeout(updateContainers, 50); // Small delay to ensure sidebar animation completes
            });
        }

        // Also observe for class changes (in case sidebar is toggled elsewhere)
        const observer = new MutationObserver(updateContainers);
        observer.observe(sidebar, { attributes: true, attributeFilter: ['class'] });
    }
}

// Initialize navbar adjustment when DOM is loaded
document.addEventListener('DOMContentLoaded', adjustNavbarForSidebar);

// Initialize Lucide icons
document.addEventListener('DOMContentLoaded', function () {
    if (typeof lucide !== 'undefined') {
        lucide.createIcons();
    }
});
