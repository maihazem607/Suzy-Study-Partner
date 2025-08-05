$(document).ready(function () {
    const subjectSelect = $('#subject-select');
    const contentSelect = $('#content-select');

    subjectSelect.on('change', function () {
        const subjectId = $(this).val();
        contentSelect.empty().append('<option value="">Loading...</option>');
        if (!subjectId) {
            contentSelect.empty().append('<option value="" disabled>-- Select a subject first --</option>');
            return;
        }
        fetch(`?handler=ContentsForSubject&subjectId=${subjectId}`)
            .then(response => response.json())
            .then(data => {
                contentSelect.empty();
                if (data.length > 0) {
                    $.each(data, (i, item) => contentSelect.append($('<option>', { value: item.id, text: item.text })));
                } else {
                    contentSelect.append('<option value="" disabled>-- No content found --</option>');
                }
            });
    });

    const generateBtn = $('#generateTestBtn');
    const generateForm = $('#generateForm');
    const quizModal = new bootstrap.Modal(document.getElementById('quizModal'));
    const quizModalBody = $('#quizModalBody');

    generateBtn.on('click', function () {
        const btnSpinner = $('#btn-spinner');
        const btnText = $('#btn-text');

        btnText.addClass('d-none');
        btnSpinner.removeClass('d-none');
        generateBtn.prop('disabled', true);

        const formData = new FormData(generateForm[0]);

        fetch('?handler=GenerateTest', {
            method: 'POST',
            body: formData
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    let quizHtml = '<form method="post" action="/MockTest/Index?handler=SubmitTest">';
                    quizHtml += $('input[name="__RequestVerificationToken"]').prop('outerHTML');

                    data.questions.forEach((q, i) => {
                        quizHtml += `<div class="futuristic-card static-light mb-4" style="padding: 1.5rem;"> 
                                            <p class="mb-3"><strong>${i + 1}. ${q.questionText}</strong></p>`;

                        q.options.forEach((opt, j) => {
                            // --- ✅ MODIFICATION IS HERE ---
                            // Reverted to the original, simpler format with just a margin for spacing.
                            quizHtml += `<div class="form-check mb-2"> 
                                                <input class="form-check-input" type="radio" name="answers[${i}]" value="${opt}" id="q${i}-opt${j}" required />
                                                <label class="form-check-label" for="q${i}-opt${j}">${opt}</label>
                                            </div>`;
                        });
                        quizHtml += `</div>`;
                    });
                    quizHtml += '<div class="d-grid"><button type="submit" class="futuristic-btn">Submit Test</button></div></form>';

                    quizModalBody.html(quizHtml);
                    quizModal.show();
                } else {
                    alert('Error: ' + data.message);
                }
            })
            .catch(error => {
                console.error('Error:', error);
                alert('An unexpected error occurred. Please try again.');
            })
            .finally(() => {
                btnText.removeClass('d-none');
                btnSpinner.addClass('d-none');
                generateBtn.prop('disabled', false);
            });
    });
});