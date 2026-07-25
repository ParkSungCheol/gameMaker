package com.example.test;

import android.content.Intent;
import android.os.Bundle;
import android.util.DisplayMetrics;
import android.view.View;
import android.view.View.OnClickListener;
import android.view.animation.Animation;
import android.view.animation.AnimationUtils;
import android.widget.Button;
import android.widget.ImageView;
import android.widget.TextView;

import androidx.appcompat.app.AppCompatActivity;
import androidx.constraintlayout.widget.ConstraintLayout;

public class MainActivity extends AppCompatActivity {

    Button mapButton = null;
    Button upgradeButton = null;
    Button gotchaButton = null;
    Button batchButton = null;
    // fade_out animation
    Animation animFadeOut = null;
    TextView fade = null;

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        overridePendingTransition(R.anim.fade_in, R.anim.fade_out);
        setContentView(R.layout.main);
        mapButton = findViewById(R.id.mapButton);
        upgradeButton = findViewById(R.id.upgradeButton);
        gotchaButton = findViewById(R.id.gotchaButton);
        batchButton = findViewById(R.id.batchButton);
        // 맵 이미지뷰 화면 높이, 너비에 맞추기
        DisplayMetrics displayMetrics = new DisplayMetrics();
        getWindowManager().getDefaultDisplay().getMetrics(displayMetrics);
        int height = this.getWindow().getDecorView().getHeight();
        int width = displayMetrics.widthPixels;
        ImageView img = findViewById(R.id.mainImg);
        ConstraintLayout.LayoutParams params = (ConstraintLayout.LayoutParams) img.getLayoutParams();
        params.width = width;
        params.height = height;
        img.setScaleType(ImageView.ScaleType.FIT_XY);
        img.setLayoutParams(params);
        // fade_out animation
        animFadeOut = AnimationUtils.loadAnimation(this, R.anim.fade_out);
        // 나타났다 사라지는 textView
        fade = findViewById(R.id.fade3);

        mapButton.setOnClickListener(new OnClickListener() {
            @Override
            public void onClick(View v) {
                // TODO convert activity to MapActivity
                Intent intent = new Intent(MainActivity.this, MapActivity.class);
                startActivity(intent);
                finish();
            }
        });

        upgradeButton.setOnClickListener(new OnClickListener() {
            @Override
            public void onClick(View v) {
                // TODO convert activity to UpgradeActivity
                Intent intent = new Intent(MainActivity.this, UpgradeActivity.class);
                startActivity(intent);
                finish();
            }
        });

        gotchaButton.setOnClickListener(new OnClickListener() {
            @Override
            public void onClick(View v) {
                alert("업그레이드 예정");
            }
        });

        batchButton.setOnClickListener(new OnClickListener() {
            @Override
            public void onClick(View v) {
                alert("업그레이드 예정");
            }
        });
    }

    // 안내문구 출력(나왔다가 사라지는 안내문구)
    public void alert(String message) {
        fade.bringToFront();
        fade.setText(message);
        fade.setVisibility(View.VISIBLE);
        fade.startAnimation(animFadeOut);
    }
}