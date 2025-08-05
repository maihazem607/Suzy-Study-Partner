document.getElementById("categorySelect").addEventListener("change", function () {
    const categoryId = this.value;
    const noteSelect = document.getElementById("noteSelect");

    if (!categoryId) {
        noteSelect.innerHTML = '<option value="">-- Select Note --</option>';
        return;
    }

    fetch(`?handler=Notes&categoryId=${categoryId}`)
        .then(res => res.json())
        .then(data => {
            noteSelect.innerHTML = '<option value="">-- Select Note --</option>';
            data.forEach(note => {
                const option = document.createElement("option");
                option.value = note.value;
                option.text = note.text;
                noteSelect.appendChild(option);
            });
        });
});

// ✅ FLASHCARD NAVIGATION FUNCTIONALITY
let currentCardIndex = 0;
let totalCards = 0;

document.addEventListener('DOMContentLoaded', function () {
    // Initialize flashcard functionality
    initializeFlashcards();

    // Add keyboard navigation
    document.addEventListener('keydown', function (e) {
        if (e.key === 'ArrowLeft') {
            e.preventDefault();
            previousCard();
        } else if (e.key === 'ArrowRight') {
            e.preventDefault();
            nextCard();
        } else if (e.key === ' ' || e.key === 'Enter') {
            e.preventDefault();
            flipCurrentCard();
        }
    });
});

function initializeFlashcards() {
    const flashcardDisplays = document.querySelectorAll('.flashcard-display');
    totalCards = flashcardDisplays.length;

    if (totalCards > 0) {
        // Add click event to current visible flashcard
        addFlipEventToCurrentCard();
        updateNavigationButtons();
    }
}

function addFlipEventToCurrentCard() {
    const currentFlashcard = document.querySelector('.flashcard-display.active .flashcard-container');
    if (currentFlashcard) {
        currentFlashcard.addEventListener('click', function () {
            this.querySelector('.flashcard-inner').classList.toggle('is-flipped');
        });
    }
}

function previousCard() {
    if (currentCardIndex > 0) {
        showCard(currentCardIndex - 1);
    }
}

function nextCard() {
    if (currentCardIndex < totalCards - 1) {
        showCard(currentCardIndex + 1);
    }
}

function showCard(index) {
    // Hide current card
    const currentCard = document.querySelector('.flashcard-display.active');
    if (currentCard) {
        currentCard.classList.remove('active');
        // Reset flip state when switching cards
        const flipCard = currentCard.querySelector('.flashcard-inner');
        if (flipCard) {
            flipCard.classList.remove('is-flipped');
        }
    }

    // Show new card
    const newCard = document.querySelector(`[data-card-index="${index}"]`);
    if (newCard) {
        newCard.classList.add('active');
        currentCardIndex = index;

        // Add flip event to new card
        addFlipEventToCurrentCard();

        // Update UI
        updateNavigationButtons();
        updateCounter();
    }
}

function flipCurrentCard() {
    const currentFlashcard = document.querySelector('.flashcard-display.active .flashcard-inner');
    if (currentFlashcard) {
        currentFlashcard.classList.toggle('is-flipped');
    }
}

function updateNavigationButtons() {
    const prevBtn = document.getElementById('prevBtn');
    const nextBtn = document.getElementById('nextBtn');

    if (prevBtn) prevBtn.disabled = (currentCardIndex === 0);
    if (nextBtn) nextBtn.disabled = (currentCardIndex === totalCards - 1);
}

function updateCounter() {
    const currentCardSpan = document.getElementById('currentCard');
    if (currentCardSpan) {
        currentCardSpan.textContent = currentCardIndex + 1;
    }
}