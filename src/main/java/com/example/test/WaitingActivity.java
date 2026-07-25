package com.example.test;

import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.view.View.OnClickListener;
import android.widget.ImageView;
import android.widget.TextView;

import androidx.appcompat.app.AppCompatActivity;
import androidx.constraintlayout.widget.ConstraintLayout;

import com.bumptech.glide.Glide;
import com.bumptech.glide.RequestManager;

public class WaitingActivity extends AppCompatActivity {

    TextView nameView;
    ImageView imageView;
    ConstraintLayout layout;
    private RequestManager glide = null;

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        glide = Glide.with(this);
        setContentView(R.layout.waiting);
        nameView = findViewById(R.id.waiting);
        imageView = findViewById(R.id.imageView);
        imageView.setScaleType(ImageView.ScaleType.CENTER);
        setViewResource("ourbasic", "move", imageView);
        // whole layout
        layout = (ConstraintLayout)findViewById(R.id.waitingLayout);
        new DBPrePopulateNotifier(this) {
            @Override
            public void onFinished() {
                nameView.setText("Please Click To Start");
                layout.setOnClickListener(new OnClickListener() {
                    @Override
                    public void onClick(View v) {
                        // TODO convert activity to MapActivity
                        Intent intent = new Intent(WaitingActivity.this, MainActivity.class);
                        startActivity(intent);
                        finish();
                    }
                });
            }
        }.execute();
    }

    public void setViewResource(String name, String action, ImageView newImg) {
        int checkExistence = getResources().getIdentifier(name + action, "drawable", getPackageName());
        if ( checkExistence != 0 ) {  // the resource exists
            glide.load(getResources().getIdentifier(name + action , "drawable", getApplicationContext().getPackageName())).into(newImg);
        }
        else {  // checkExistence == 0  // the resource does NOT exist
            newImg.setImageResource(getResources().getIdentifier(name, "drawable", getApplicationContext().getPackageName()));
        }
    }
}