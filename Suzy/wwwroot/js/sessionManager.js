let currentTab = 'public';

// Initialize page
document.addEventListener('DOMContentLoaded', function () {
    loadPublicSessions();
    setupEventListeners();
});

function setupEventListeners() {
    // Timer type change handler
    document.querySelectorAll('input[name="timerType"]').forEach(radio => {
        radio.addEventListener('change', function () {
            const customInputs = document.getElementById('customTimerInputs');
            if (this.value === '2') { // Custom
                customInputs.classList.add('show');
            } else {
                customInputs.classList.remove('show');
            }
        });
    });

    // Form submission
    document.getElementById('sessionForm').addEventListener('submit', function (e) {
        e.preventDefault();
        createSession();
    });
}

function switchTab(tab) {
    // Update tab buttons
    document.querySelectorAll('.tab-button').forEach(btn => btn.classList.remove('active'));
    event.target.classList.add('active');

    // Update tab content
    document.querySelectorAll('.tab-content').forEach(content => content.classList.remove('active'));
    document.getElementById(tab + 'Tab').classList.add('active');

    currentTab = tab;

    // Load data for the tab
    if (tab === 'public') {
        loadPublicSessions();
    } else {
        loadMySessions();
    }
}

async function loadPublicSessions() {
    try {
        const response = await fetch('/api/StudySession/GetAllAvailableSessions');
        if (response.ok) {
            const sessions = await response.json();
            renderSessions(sessions, 'publicSessions', false);
        } else {
            console.error('Failed to load available sessions');
        }
    } catch (error) {
        console.error('Error loading available sessions:', error);
    }
}

async function loadMySessions() {
    try {
        const response = await fetch('/api/StudySession/GetMySessions');
        if (response.ok) {
            const sessions = await response.json();
            renderSessions(sessions, 'mySessions', true);
        } else {
            console.error('Failed to load my sessions');
        }
    } catch (error) {
        console.error('Error loading my sessions:', error);
    }
}

function renderSessions(sessions, containerId, isMySession) {
    const container = document.getElementById(containerId);

    if (sessions.length === 0) {
        container.innerHTML = `
                        <div class="empty-state">
                            <i class="fas fa-users fa-2x"></i>
                            <p class="mt-2">${isMySession ? 'You haven\'t created any sessions yet.' : 'No sessions available.'}</p>
                        </div>
                    `;
        return;
    }

    container.innerHTML = sessions.map(session => `
                    <div class="session-card">
                        <div class="session-header">
                            <div class="session-title">${session.title}</div>
                            <div class="session-badge ${session.isPublic ? 'badge-public' : 'badge-private'}">
                                ${session.isPublic ? 'Public' : 'Private'}
                                ${session.requiresCode && !isMySession ? ' - Code Required' : ''}
                            </div>
                            ${isMySession ? `
                                <div class="session-actions">
                                    <button class="btn btn-sm btn-outline-danger" 
                                            onclick="event.stopPropagation(); deleteSession(${session.id}, '${session.title}')"
                                            title="Delete Session">
                                        <i class="fas fa-trash"></i>
                                    </button>
                                </div>
                            ` : ''}
                        </div>
            
                        <div class="session-clickable" onclick="${isMySession ? `joinSession(${session.id})` : (session.requiresCode ? `joinPrivateSession(${session.id}, '${session.title}')` : `joinSession(${session.id})`)}">
                            ${session.description ? `<p class="text-muted mb-2">${session.description}</p>` : ''}
                
                            <div class="timer-info">
                                <div class="timer-type">
                                    ${getTimerTypeDisplay(session.timerType, session.studyDuration, session.breakDuration)}
                                </div>
                                <div class="participants-info">
                                    <i class="fas fa-users"></i>
                                    ${session.currentParticipants}/${session.maxParticipants}
                                </div>
                            </div>
                
                            ${isMySession && !session.isPublic ? `
                                <div class="mt-2">
                                    <small class="text-muted">Invite Code: <strong>${session.inviteCode}</strong></small>
                                </div>
                            ` : ''}
                    
                            ${session.requiresCode && !isMySession ? `
                                <div class="mt-2">
                                    <small class="text-info">
                                        <i class="fas fa-lock me-1"></i>
                                        Click to enter invite code
                                    </small>
                                </div>
                            ` : ''}
                        </div>
                    </div>
                `).join('');
}

function getTimerTypeDisplay(timerType, studyDuration, breakDuration) {
    switch (timerType) {
        case 0: return 'Pomodoro (25/5)';
        case 1: return 'Flowmodoro';
        case 2: return `Custom (${studyDuration}/${breakDuration})`;
        default: return 'Unknown';
    }
}

function showCreateForm() {
    document.getElementById('createSessionForm').style.display = 'block';
    document.getElementById('sessionTitle').focus();
}

function hideCreateForm() {
    document.getElementById('createSessionForm').style.display = 'none';
    document.getElementById('sessionForm').reset();
    document.getElementById('customTimerInputs').classList.remove('show');
}

async function createSession() {
    const formData = {
        title: document.getElementById('sessionTitle').value,
        description: document.getElementById('sessionDescription').value,
        isPublic: document.querySelector('input[name="visibility"]:checked').value === 'true',
        maxParticipants: parseInt(document.getElementById('maxParticipants').value),
        timerType: parseInt(document.querySelector('input[name="timerType"]:checked').value),
        studyDuration: parseInt(document.getElementById('studyDuration').value),
        breakDuration: parseInt(document.getElementById('breakDuration').value)
    };

    try {
        const response = await fetch('/api/StudySession/CreateSession', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(formData)
        });

        if (response.ok) {
            const result = await response.json();
            if (result.success) {
                hideCreateForm();

                if (result.inviteCode) {
                    alert(`Session created! Invite code: ${result.inviteCode}`);
                } else {
                    alert('Session created successfully!');
                }

                // Redirect to the session
                window.location.href = `/Sessions/StudySessions?sessionId=${result.sessionId}`;
            }
        } else {
            alert('Failed to create session. Please try again.');
        }
    } catch (error) {
        console.error('Error creating session:', error);
        alert('An error occurred. Please try again.');
    }
}

async function joinSession(sessionId) {
    try {
        const response = await fetch('/api/StudySession/JoinSession', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ sessionId: sessionId })
        });

        if (response.ok) {
            const result = await response.json();
            if (result.success) {
                window.location.href = `/Sessions/StudySessions?sessionId=${result.sessionId}`;
            } else {
                alert(result.message || 'Failed to join session');
            }
        } else {
            alert('Failed to join session. Please try again.');
        }
    } catch (error) {
        console.error('Error joining session:', error);
        alert('An error occurred. Please try again.');
    }
}

async function joinPrivateSession(sessionId, sessionTitle) {
    const inviteCode = prompt(`Enter the invite code for "${sessionTitle}":`);
    if (!inviteCode) {
        return; // User cancelled
    }

    try {
        const response = await fetch('/api/StudySession/JoinSession', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                sessionId: sessionId,
                inviteCode: inviteCode.trim().toUpperCase()
            })
        });

        if (response.ok) {
            const result = await response.json();
            if (result.success) {
                window.location.href = `/Sessions/StudySessions?sessionId=${result.sessionId}`;
            } else {
                alert(result.message || 'Invalid invite code. Please try again.');
            }
        } else {
            alert('Failed to join session. Please check your invite code and try again.');
        }
    } catch (error) {
        console.error('Error joining private session:', error);
        alert('An error occurred. Please try again.');
    }
}

async function joinWithCode() {
    const inviteCode = document.getElementById('inviteCodeInput').value.trim();
    if (!inviteCode) {
        alert('Please enter an invite code');
        return;
    }

    try {
        const response = await fetch('/api/StudySession/JoinSession', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ inviteCode: inviteCode })
        });

        if (response.ok) {
            const result = await response.json();
            if (result.success) {
                window.location.href = `/Sessions/StudySessions?sessionId=${result.sessionId}`;
            } else {
                alert(result.message || 'Failed to join session');
            }
        } else {
            alert('Failed to join session. Please try again.');
        }
    } catch (error) {
        console.error('Error joining session:', error);
        alert('An error occurred. Please try again.');
    }
}

async function deleteSession(sessionId, sessionTitle) {
    if (!confirm(`Are you sure you want to delete the session "${sessionTitle}"?\n\nThis action cannot be undone and will remove all participants from the session.`)) {
        return;
    }

    try {
        const response = await fetch(`/api/StudySession/DeleteSession/${sessionId}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
            }
        });

        if (response.ok) {
            const result = await response.json();
            if (result.success) {
                alert('Session deleted successfully');
                // Refresh the sessions list
                loadMySessions();
            } else {
                alert(result.message || 'Failed to delete session');
            }
        } else {
            alert('Failed to delete session. Please try again.');
        }
    } catch (error) {
        console.error('Error deleting session:', error);
        alert('An error occurred while deleting the session. Please try again.');
    }
}